# Cab-performance audit fixes — 31 October 2026

James authorised fixing the audit findings, beginning with axle allocations,
repeated pipe exceptions and exhaust audio cleanup. Preserve saves, use agents,
test without loading Derail Valley, and leave the candidate open for more work.

## Scope and compatibility

- The active tree already contained uncommitted 0.22.3 geometry, lighting and
  oiling changes. This pass does not replace or revert those edits.
- The bogie getter now retains its existing array when every axle remains live.
  It filters only when needed, preserving survivor order and updating both the
  native cache and the first caller's result in the same frame.
- Invalid/overlapping steam-pipe poses retain their last valid visual and recover
  on the next valid pose. Finite-value/frame checks precede Unity mesh writes.
  A conservative displacement budget is measured from the last uploaded mesh,
  avoiding repeated tiny-motion rebuilds without accumulating fitting error.
- Exhaust initialization and teardown are transactional. Partial native event
  subscriptions, second Dockside exhaust readers, loop state and owner enrollment
  are cleaned up even when another step fails. Repeated managed-reader Init and
  Deinit calls do not duplicate subscriptions or copies. Original failures remain
  visible; independent cleanup failures are aggregated rather than discarded.
- No save schema, arrangement ID, engine/oil circuit ID, simulation calibration
  or resource/damage data format changes. New pose/lifecycle state is transient.
- No asset bundles, package versions or installed game files are changed by this
  pass. Renderer consolidation and animation-culling changes still require a
  representative frame capture; renderer count alone is not a measured defect.

## Validation

Before the fixes, the runtime compiled with zero warnings/errors. The existing
native suites passed: exhaust 611, cocks 248, cleanup 145, collision 7062,
contacts 21, conventional 531, quartering 115 and Shay effects 23. Baseline
evidence: `build/radio-0.22.3/runtime-audits-20261031-210532`.

The first sandboxed Unity launch failed before test execution because Windows
hardware identity was inaccessible to licensing. Running the same isolated
fixtures with normal host access resolved that environment issue. No licence
files or activation settings were changed.

Focused axle tests cover unchanged-array identity, order, missing transforms,
dynamic retirement and the existing 100 same-frame retirement cases. 100,000
repeated getter/postfix calls allocate zero managed bytes in the fixture. Unity
activity in this focused test is doubled; native tests provide separate coverage.

Final candidate SHA256:
`09E58638D664DECDF2727FE615370249533D62A49C51432933E5BCF45776C225`.

All ten native suites passed against that exact DLL (9,989 assertions):

| Suite | Checks |
|---|---:|
| Existing exhaust | 611 |
| New exhaust failure/repeated lifecycle | 115 |
| New pipe recovery/fitting budget | 1,118 |
| Cylinder cocks | 248 |
| Cleanup | 145 |
| Meyer collision | 7,062 |
| Actual native contacts | 21 |
| Conventional/native wheel geometry | 531 |
| Actual Unity quartering | 115 |
| Shay effects | 23 |

Exact-hash evidence:
`build/radio-0.22.3/runtime-audits-20261031-211452/runtime-validation.json`.
The audio failure fixture deliberately generates exactly two native missing-port
diagnostics and asserts them. Injected exceptions must preserve their original
cause through cleanup; they are not hidden to obtain a passing result. The
initial fixture failures came from an empty LayeredAudio layer list (native
Reset intentionally returns early) and a missing native flow host for error
formatting. Both fixtures were completed and the original assertions retained.
The unchanged Shay effects fixture also deliberately reproduces the historical
active-clone LayeredAudio/Doppler errors before checking the production inactive
construction path. Its final production path passes without activation errors.

Managed/native component suites also passed: functional/save 193, oil migration
and masking 628, engine/save/ECS slip 351, bounded trace 145, conversion boundary
35, compound physics 25 and geometry/native length 1,151, plus the axle checks
above. Logs: `build/perf-fixes-20261031/managed`.

Runtime compilation: zero warnings/errors. The authoring helper also compiles.
Two pre-existing missing Bodywork references in Lights blocked the first helper
build; shared R2 bulkhead datums now live in the already-shared MeyerPlan with
identical values, and Bodywork retains its existing aliases. No additional game
dependency was added to authoring. Its full compile retains existing DTO/unused
field warnings; they were not suppressed. PowerShell parsing and diff whitespace
checks pass.

No customer log/profiler capture is available, so fixes to the demonstrated
mechanisms do not establish the cause or FPS impact of the original lag report.

## Handoff state

Leave the candidate unreleased and uninstalled for further changes. Do not run
Build.Release, packaging, installation or publication as an inferred next step.
Test.Runtime includes the new candidate-DLL pipe and exhaust failure-injection
fixtures; future Build.Radio evidence must include both reports.
