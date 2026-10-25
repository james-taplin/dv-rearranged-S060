# Rearranged S060

Change the S060's running gear through a dedicated comms-radio mode in Derail
Valley. This project forks the Rearranged S282 B99/Re:Arranged branch, based on
Peter Schinske / SatiricRug's original Rearranged S282.

## Current candidate: S060 0.19.1, Meyer/Dockside packs 0.7.0

Version 0.19.1 preserves native drivetrain phases while Meyer/Dockside hide the
original mechanisms, then restores the original Animator settings on return.
Oil-save migration now uses the native per-component loader and accepts only
known six/twelve-point formats; current fourteen-point data and unknown arrays
are not rewritten. No save schema, circuit ID, simulation or targeting change.

Version 0.19.0 adds a conventional **0-10-0 Tank** with five real powered axles,
fixed 1.00 m drivers, fitted coupling rods and ten functional big-end oil cups.
It is available through the existing radio/UMM controls and an optional spawn
pool entry. The fifth wheel and oil reservoirs append persistent state without
renumbering existing arrangements or circuits. The new layout needs in-game
acceptance, especially on tight curves; it is included as a public beta.

This candidate routes the Meyer rear LP exhaust return above the cab roof edge
before it descends to the forward smokebox run. Period-style ball/slip joints
and roof/footplate brackets support it. The locomotive retains **one chimney**;
the rear unit has no separate exhaust stack. The fictional pipe arrangement is
not an exact historical replica.
Final articulation/clearance evidence and gameplay acceptance are recorded in
the handover rather than inferred from its straight-track appearance.

Owned cylinder-cock readers also refresh when their inlet-valve mask changes,
including a stopped crank with unchanged steam flow. This retains native valve
timing and emission thresholds: an LP unit near atmospheric pressure may have
too little cock flow for visible jets, and a dry cylinder produces no water
drips. Automatic diagnostics now re-arm for each changed arrangement and include
cylinder water/temperature, health/windows and body/bogie motion/joint snapshots.

The rear shell now belongs to the existing native rear-bogie Rigidbody. Previously,
its colliders were moved inside the main car's compound each frame, which could
change the car's automatic centre of mass and inertia as the bogie moved. Native
collision readiness and impact callbacks are retained. This corrects a demonstrated
ownership defect; the reported window damage and stationary bump still need a
gameplay retest with the expanded diagnostics.

The preceding effects update used discordraconequus's Verde authoring pack as a
reference. Cock steam/drip/sound holders follow measured cylinder drain caps;
owned radio particle readers use their actual crank for flow/reverser callbacks.
Native activity curves, inlet masks and gains remain. Motion checks cover reverse
travel, crank wrapping and independent unit clocks; no Franklin cutoff actuator
animation is added.

The earlier responsibility separation, retryable cleanup and archived prototype
source remain. Existing saved IDs, compound simulation and
calibration precision are retained; this update adds the conventional 0-10-0 without changing resource capacity.
Version-1/version-2 saves remain readable. A fifth wheel uses version-3 wheel
condition data; existing oil circuit IDs stay fixed and two new circuits append.

Final candidate validation and installation are recorded in
[the handover](docs/HANDOVER.md). Source changes and passing isolated checks do
not establish gameplay acceptance. See [the test guide](docs/RADIO-ARTICULATED-TESTING.md).

## Arrangements

- **0-6-0 Stock:** original 1.42 m drivers.
- **0-4-0:** four-coupled running gear with stock-sized drivers.
- **0-6-0 Short Wheelbase:** closer axle spacing and 1.14 m drivers.
- **0-8-0:** four fitted axles using the same 1.14 m drivers.
- **0-6-2:** stock drivers with a native trailing pony axle.
- **0-10-0 Tank:** five powered axles, 1.00 m drivers and ten big-end oil cups.
- **Meyer-style 0-4+4-0T:** two powered axles beneath the S060, plus two beneath
  an attached pannier rear section, with HP -> receiver -> LP compound steam.
- **Franklin Dockside 0-4-4-0T:** two simple-expansion units beneath the tank body,
  1.00 m drivers and animated Franklin gear.

The Meyer is one S060 identity. Changing away removes its rear section and
restores the stock supports, rear coupling point and length. Its panniers are
visual and do not increase water capacity. Dockside has no rear extension.

Choices have fixed, fitted wheel sizes. There is no release wheel-size slider.
Switching retains the native locomotive identity, livery, cab, fuel/water,
damage and save ownership. Both articulated cylinder-cock sets use the existing
cab control. Each driven axle has two functional big-end oil cups alongside the
four original guide/crosshead lubricators.

Meyer exhaust presentation follows its LP stage, with lower-pressure chuff bands
informed by discordraconequus's Verde compound. Its new return is separate from
the existing admission/interstage pipework. Both Dockside units contribute
exhaust events to its single chimney. Native readers still control particles and
sound; the visible Meyer return does not change compound simulation or resources.

## Requirements and installation

Developed against Derail Valley **B99 build2702**, Unity Mod Manager, Loco Mesh
Splitter and Custom Car Loader. S282 is not a runtime dependency. The authoring
workflow does need its custom Franklin source bundle; see [BUILDING.md](BUILDING.md).

The Nexus download is one ZIP to drag directly into UMM. It installs one
`RearrangedS060` mod folder, containing the runtime and the two native CCL bundles
under `assets/Meyer` and `assets/Dockside`. There are no nested ZIPs. CCL is a
normal dependency; Rearranged S282 is not required.

If upgrading a previous three-part test install, first uninstall the old
`RS060_Meyer_0440` and `RS060_Franklin_Dockside_0440` entries through UMM. Keep
`RearrangedS060` and its settings, then install the combined ZIP. This one-time
step avoids duplicate asset IDs. See [installation instructions](docs/nexus/INSTALL.txt).

Close the game before updating. The runtime and asset bundles retain independent
version numbers. Repository development packages/installer retain their older
three-folder layout; use `Package.Nexus.ps1` for the public single-mod archive.
The repository's installer validates package hashes
and refuses a running game; it does not publish a release. The installer now
creates no backup, as requested; it records prior hashes and verifies installed
files. Old S060 backup copies have been removed. Exact cleanup and installed
state are recorded in the handover.

Stop and close the regulator before selecting a layout. Before entering or
leaving Meyer, uncouple the rear, disconnect its hose and leave clear track.
Follow [the test guide](docs/RADIO-ARTICULATED-TESTING.md) for servicing,
streaming, curves and save/reload checks.

## Development

Use `Build.Runtime.ps1` for the current DLL and `Build.Release.ps1` for a tested
complete development package, then `Package.Nexus.ps1` for the single UMM ZIP.
The project builds only the current product (Stage 7);
requesting historical stages fails explicitly.

[BUILDING.md](BUILDING.md) describes local paths, SDK/Unity dependencies, native
fixture preparation, hash-bound evidence and the separate installation step.
Native game body/wheel/texture fixtures remain in ignored local QA outputs and
are not distributed. Current packs include inherited custom Franklin/pipe meshes
and alias native S060 materials at runtime.

Seventy-four inactive source/resource/script files were moved byte-for-byte to
[archive/legacy-runtime](archive/legacy-runtime/README.md). Its manifest records
original paths and hashes. Archived scripts are reference material, not supported
build commands. Historical measurements, decisions and earlier prototype
results are retained in [docs/FEASIBILITY.md](docs/FEASIBILITY.md),
[docs/TESTING.md](docs/TESTING.md) and [docs/HANDOVER.md](docs/HANDOVER.md).

## Licence and credit

Retain the MIT copyright/licence notices in [LICENSE](LICENSE), including Peter
Schinske's original work. See [CREDITS.md](CREDITS.md) for attribution and the
Verde reference credit to **discordraconequus**. Credits do not replace the
respective third-party asset permissions. No Verde art or audio is distributed.
