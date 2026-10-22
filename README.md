# Rearranged S060

Change the S060's running gear through a dedicated comms-radio mode in Derail
Valley. This project forks the Rearranged S282 B99/Re:Arranged branch, based on
Peter Schinske / SatiricRug's original Rearranged S282.

## Current candidate: S060 0.16.0, Meyer/Dockside packs 0.7.0

This candidate follows the 0.15.0 audit cleanup, using discordraconequus's Verde
authoring pack as a reference for native effects and motion patterns. Cock
steam/drip/sound holders now follow measured cylinder drain caps. Owned radio
particle readers use their actual crank when flow or reverser events arrive,
fixing a native callback-value phase hazard on stopped engines. Native activity
curves, inlet masks and gains remain. Reverse travel, crank wrapping and
independent unit clocks receive motion checks; no new Franklin cutoff actuator
is claimed.

The earlier responsibility separation, retryable cleanup and archived prototype
source remain. Existing saved IDs, engine-save schema, compound simulation and
calibration precision are retained; this work adds no arrangements or resources.

Final candidate validation and installation are recorded in
[the handover](docs/HANDOVER.md). Source changes and passing isolated checks do
not establish gameplay acceptance. See [the test guide](docs/RADIO-ARTICULATED-TESTING.md).

## Arrangements

- **0-6-0 Stock:** original 1.42 m drivers.
- **0-4-0:** four-coupled running gear with stock-sized drivers.
- **0-6-0 Short Wheelbase:** closer axle spacing and 1.14 m drivers.
- **0-8-0:** four fitted axles using the same 1.14 m drivers.
- **0-6-2:** stock drivers with a native trailing pony axle.
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
informed by discordraconequus's Verde compound. Both Dockside units contribute
exhaust events to its single chimney. Native readers still control particles and
sound; the mod changes their per-car sources.

## Requirements and installation

Developed against Derail Valley **B99 build2702**, Unity Mod Manager, Loco Mesh
Splitter and Custom Car Loader. S282 is not a runtime dependency. The authoring
workflow does need its custom Franklin source bundle; see [BUILDING.md](BUILDING.md).

The complete package contains three mod folders:

- `RearrangedS060` — runtime, radio, simulation and persistence.
- `RS060_Meyer_0440` — Meyer asset pack.
- `RS060_Franklin_Dockside_0440` — Dockside asset pack.

Close the game before updating all three. The runtime and asset packs have
independent version numbers. The repository's installer validates package hashes
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
complete package. The project builds only the current product (Stage 7);
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
