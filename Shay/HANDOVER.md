# Shay geared exhaust — 13 November 2026

James requested **fix Shay only, commit only** after reporting harsh, shrill,
thrumming S060 donor exhaust above 10 km/h. No installation, release packaging,
push or DV launch was performed. Dryope audio and engine simulation are unchanged.

The existing native `ChuffClipsSimReader` now receives original Shay sound data
only while the engine bank selects Shay. Three pressure families contain six
ordered exhaust phases with two takes each. Eight native running-loop rates
(4/8/16/24/32/48/64/96 beats/sec) replace extreme stretching of the stock 16 Hz
bed. Playback continues to use actual native geared exhaust frequency, including
slip; it does not divide the rhythm to make the engine sound slower.

The 60 original mono 24 kHz PCM WAVs are generated from broad filtered turbulent
noise with short rounded releases, subtle six-phase accents and no metal impacts,
pure tone or baked reverb. This is an authored sound design, not a historical
recording or a claim of measured acoustic fidelity. `Audio/generate.py` recreates
the committed WAVs using NumPy; `--check` validates the committed files without
rewriting them. Waveform checks cover headroom, DC, joins and high-frequency energy.

Native DV still handles pressure, pooled one-shots, loop mixing, Doppler and cab
mixers. Native water/ash configuration is preserved. Each reader owns its new
sources/configuration and restores the exact original references on streaming or
selection changes. Shared S060 donors are not edited. The installed Toolbox
source uses a last-priority Init prefix, so its bank adaptation sees the Shay
replacement; its stock-clip pitch/cadence adapters exclude the original clips.
This interoperation was inspected statically, not exercised in a running game.

Audio is imported into the existing `shay_running_gear` bundle; no extra runtime
dependency, audio scheduler or new deployment file is introduced. Later packaging
must include **both** the updated DLL and rebuilt Shay bundle. The old bundle
does not contain these recordings. Standard `Build.Release.ps1` calls `Build.Shay`;
any manually assembled candidate based on an older package must replace its Shay
bundle explicitly. Existing staged Dryope packages were not updated by this task.

## Offline evidence

- Before editing: existing native exhaust/cleanup fixtures passed 611/115 checks
  at `../build/radio-0.4.4/runtime-audits-20261113-224100`.
- Runtime build: zero warnings/errors. DLL SHA256:
  `7C08C562DBF6FE678DCCDB457F620229DE19DC5CB23975D51964D16CD557AB9B`.
- One integrated Shay export: `../build/shay/bundle-20261113-224655`; bundle
  SHA256 `B52A428632D89E937414C8138A94646972A2D3AF6F29BF6656E3BD0348978A05`.
  Reopened bundle includes all 60 original clips and the existing model/motion
  contract. Existing R3 geometry check passed 11,322 assertions.
- Focused native fixture: `../build/shay/exhaust-20261113-224939`, 6,073 passed.
  Actual candidate hooks, native ports/reader/loop code, reopened clips, 0.1–192 Hz
  gain coverage, correct cadence, all pressure/phase indices, three selection and
  streaming cycles, exact restoration, unchanged Dryope references and atomic
  missing-data rejection. Inactive host; final native one-shot pool intercepted.
  Does not validate audible output, Doppler ECS activation or actual gameplay.
- Existing native exhaust/cleanup fixtures rerun against this exact DLL:
  611/115 passed at `../build/radio-0.4.4/runtime-audits-20261113-225049`.

The first sandboxed baseline could not access Unity licensing; the same hidden
Editor checks passed outside the sandbox without changing licence state. The
first focused fixture referenced NAudio in the wrong assembly; correcting its
lookup to DV.NAudio.dll passed. Neither fixture correction rebuilt production.
The compiled tree includes pre-existing pending 0.4.4 work; this commit includes
only the Shay sound changes, related build/check changes and this handover.

Recheck with `Build.Runtime.ps1`, `Build.Shay.ps1`, then
`Shay/Test.Exhaust.ps1 -Bundle <new shay_running_gear>` and
`Test.Runtime.ps1 -SkipBuild -Only exhaust,exhaust-failure`. James's later listening
acceptance should cover a warm Shay under light/heavy power at crawl, 10 and
20 km/h, coasting, stop/reverse, sound streaming and switching back to stock.
