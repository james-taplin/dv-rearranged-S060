# 0.3.3 Nexus UMM release — 14 November 2026

James explicitly requested a clean 0.3.3 release ZIP, a code check, plain
ASD-STE100-style documentation and installation of that package. This replaces
the earlier installation rescission. The verified package is now installed,
without a backup. Settings.xml was preserved. No game launch, gameplay test,
push or publication was performed. James owns gameplay acceptance.

## Source and confirmed repair

The parent is the staged integration checkpoint 181ddd678114e0f0511f91d07dc180c8cdb6df6c.
It already includes the scoped changes from a008bee, 08c8500 and 5c12c1d.
The source remains the isolated export at
`B:/GitHub repo/rearranged-s060-032-polish-20261113/source`.
Its directory name is historical; release metadata and assembly are 0.3.3.
The separate Documents v0.4-work is not an input or output of this release.

Code review covered the integrated Shay audio and mesh lifetime, Meyer pipe
route, Franklin geometry and crank removal, 0-4-0 equipment, conversion cleanup,
and the existing package and installation paths. One defect was confirmed:
the 0-4-0 air-equipment owner removed fixed gear before scaled cylinder fitting
made another body-mesh copy. Cleanup released those copies out of order and
could leave the body without its original mesh after leaving scaled 0-4-0.

The regression fixture was first corrected to use the actual body-pipe and
fixed-gear owner sequence. It failed against the previous exact runtime with
“Leaving 040 must restore the exact native body mesh.” The failed evidence and
old DLL are retained under `build/release-0.3.3/040-baseline` and
`build/release-0.3.3/before-review.dll`.

The repair reuses S060DriverScale's existing fixed-gear owner, as Dockside does.
Retirement now runs after cylinder fitting; Scale.Restore releases the copies
in reverse order. The air-equipment owner controls only its generated parts.
No new hook, service, dependency, simulation change or asset rebuild was needed.
The working animated reverser remains visible. The corrected fixture passes
at stock, minimum and maximum scales, including repeat disposal and restoration.
No other defect was confirmed in this bounded review; this is not a claim that
every possible game state is bug-free.

## Public package and documents

The ZIP has exactly 12 files under one RearrangedS060 folder: the runtime,
Info.json, three asset bundles, README, installation instructions, complete
changelog, two asset readmes, credits and the unchanged licence.
It contains no source, Git data, nested packages, QA reports or testing notes.
Author, mod identity, requirements and entry point remain unchanged.

The public documents use short active sentences, consistent terms and separate
procedure steps. They follow ASD-STE100 style; no formal dictionary certification
is claimed. Reference: https://www.asd-ste100.org/STE_faq.html and Issue 9 of
the official specification. Procedure steps are at most 20 words; descriptive
sentences are at most 25. The actual ZIP text was checked for Git references,
markup, BBCode and internal validation/candidate notes. Legal text was retained.

The changelog includes all recorded releases from 0.1.0 through 0.3.3, including
the earlier version sequence. Date-labelled headings distinguish the original
0.3.0/0.4.0 releases from the later combined 0.3.0 line. Historical 0.4.0 is not
the separate current 0.4 work. History provenance is retained privately in
`build/release-0.3.3/release-history-source.json`.

## Validation and artifact identity

The corrected release DLL built with zero warnings and zero errors.
All checks below use that exact runtime or its extracted package:

- 16 native runtime suites: 28,602 checks.
- Existing private independent-drive player fixture: 101 checks.
- Shay audio: 6,037 checks; 0-4-0 equipment: 2,118; Franklin: 802.
- Native UMM extraction: 60 checks; extracted Unity bundles: 39.
- Fake-game installation fixtures: 19 combined-package and 24 legacy checks.
- All 12 installed files match the final ZIP; saved settings match the pre-install hash.

Unchanged Shay, Meyer and Dockside asset authoring evidence remains applicable
with explicit bundle hashes in the package receipt. No unchanged production
asset was rebuilt merely for documentation or fixture changes.
These are offline, managed/native fixture, geometry and importer checks.
They do not establish audible playback quality or gameplay acceptance.

Runtime SHA256:
`58021510E7E4C5529E6A3C3B4FB7762A752E5F4D8CA5FA16784F5BBC06437699`

ZIP SHA256:
`22DCE9A8239D3646B574E59AE727DA88C5EAD81191103B4CFBBA4F27AC4C4924`

Delivery:
`C:/Users/james/Documents/ChatGPT/REARRANGED S060/outputs/RearrangedS060-0.3.3-UMM.zip`

The matching plain changelog and Nexus descriptions are beside that ZIP.
Private evidence: `build/release-0.3.3/validation.json`, `install-receipt.json`
and `source-checkpoint.json`. Native reports are linked by hash in validation.json.
The exact ZIP was verified against staging and every installed payload file.
Install.Nexus.ps1 preserved Settings.xml and unrelated mod metadata and removed
only this runtime's stale assembly cache. No backup was created.

James's remaining in-game checks: change into and out of 0-4-0, listen to Shay
exhaust under power/coasting, inspect Meyer access and the return pipe, and check
Franklin motion. Do not automate these or launch DV without a new explicit request.
