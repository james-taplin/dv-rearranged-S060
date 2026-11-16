Release update, 16 November 2026: the exact tested DLL is now packaged.
Installation remains pending by James's explicit choice after DV started.
Read [RELEASE-0.3.6-mp-20261116.md](RELEASE-0.3.6-mp-20261116.md).
The commit-only status below records the preceding implementation task.

# Conventional native brakes — commit only, 16 November 2026

James requested restoring native heated brake shoes and calipers on the 0-10-0,
0-8-0 and short-wheelbase 0-6-0, with renders, progress and questions for blockers.
He selected the current 0.3.6-mp line when asked which source line to change.
Version remains 0.3.6-mp. No installation, release ZIP, push or publication is
part of this task. James owns gameplay and live peer testing.

## Confirmed cause and native repair

S060DriverScale deliberately disabled body-mounted brake renderers when scaling
the drivetrain, since stock geometry no longer fitted the smaller wheels. The
installed LMS supplies separate calipers at both static LODs, numbered front to
rear, opposite the wheel numbering. Native shoes remain one readable mesh with
six disconnected shoes. Native resources confirm readable body LOD0/LOD1 and shoes.

The working 0-4-0 shoe repair and Meyer native heat integration were inspected
first. No new controller, renderer, shader, dependency or simulation is needed.
S060ConventionalBrakes fits owned mesh copies after the wheel layout and cylinder
placement. Each shoe/caliper section scales in Y/Z about its actual source axle
onto the live wheel station; X/gauge is retained. Extra axles reuse native rear
sections within the original shoe and rear-caliper renderers. UVs, transformed
normals/tangents, material slots, LODs and paint/heat references remain native.
No per-frame update or new serialized state is added.

Normal cleanup, rollback and destruction restore exact original meshes and release
copies. Originally disabled renderer states are retained. Only S060Rescaled and
S060EightCoupled (including S060TenCoupled) receive the fitted brakes. Other
arrangements retain their existing paths.

## Validation and provenance

One production build: zero warnings/errors. Core SHA256:
`4379CFCE72ED1203B4D304205EA5A722BC612566F595499423BE2780148C4ED1`.

The bounded fixture reuses ConventionalNativeAudit's hierarchy and calls the
installed LMS GetCaliper methods on the exact native LOD0/LOD1 mesh exports.
It first reproduced the three hidden-brake layouts with the installed DLL:
85 baseline checks, excluded from candidate totals.

- 300 candidate native checks: both caliper LODs, 6/8/10 shoes, exact caliper
  section counts/material slots, shading/UV data, native glow set/clear, original
  visibility, exact mesh restoration, repeated cleanup and destroyed-filter release.
- 168 brake/wheel surface comparisons at the exported rest pose. Eighteen
  caliper/own-wheel contacts reproduce with the exact unmodified native profiles;
  they are recorded separately. No new contact pairs were found. This is not a
  claim of zero stock intersections or a complete rod/cylinder/animation sweep.
- 45 conversion-boundary checks across 20 injected scenarios. Scene discovery
  and native simulation are test doubles; their scale double gains the new entry point.
- 49 managed multiplayer-core checks, with existing boundary doubles; no live peers.

The first contact check rejected all contacts. Exact unmodified source comparison
distinguished inherited contacts without changing production or numerical tolerances.
Subsequent fixture-only checks and renders use the same DLL; no rebuild was needed.

The first sandboxed Unity launch failed WMI/machine licence checks before loading
the project. Normal system access succeeded; no licence files were changed. Two
existing unused-field/local warnings remain in unrelated Unity fixture code.

Evidence: `build/conventional-brakes-20261116/validation-summary.json`; `baseline`
contains the installed before renders; `final` contains the final validation and
cold/native-heat renders for all three arrangements. Earlier attempts retain their
own reports. Heat pictures apply an explicit preview colour through the native
controller's property-block method; they do not test temperature integration.
Local native exports/renders remain ignored and must not be packaged as game art.

Repeat:

    ./Preview.ConventionalBrakes.ps1 -RuntimePath build/stage7/RearrangedS060.dll -OutputDirectory build/conventional-brakes-20261116/recheck

Awaiting James's gameplay tests after separately authorized installation: inspect
both sides while braking, actual heat/glow development and cooling, arrangement
switches back to stock, and save/reload. No DV launch was performed.

The existing final 0.3.6-mp ZIP remains the reinstall/rollback source. No backup,
duplicate installation or new ZIP was created. Installed runtime remains SHA256
`E3959B44D4324AFEBEFCC8C2BA23C0A8FD534043952861EA3466D68C5AB6E252`.
