# Meyer link-and-pin correction — 13 November 2026

James rejected the previous footplate interpretation of the missing connection.
There is no foot access across this gap. The required visible connection is a
heavy link and pin with short drawgear underneath. The screenshot follow-up
also identified the projecting upper bearer as part of the unwanted bridge.
Do not retain or restore that bearer across the gap under another name.

This revision is **staged only**, with original 0.3.1 metadata. It starts from
the preserved repair checkpoint `120a67b5325d10b25555e8596f3c3a5cdace5e49`,
in `B:/GitHub repo/rearranged-s060-031-link-pin-20261113/source`. The previous
repair was subsequently installed by another authorised session (installed DLL
`4F09184300863A26018BE77317A452575A83789F0F7A3FDB344CC9793E212EDA`). That
installation is the read-only baseline here; it does not authorise installation
of this revision. Documents 0.4 work and the canonical checkout remain separate.

## Final model

- Full-width plate, raised lips, both upper bearers and their flanges terminate
  at the tank front, Z -4.38. No upper member crosses the visible gap. The common
  collision/walking/item plate factory ends at the same edge.
- A 675 mm eye-to-eye, 60 mm thick centre drawbar has 250 mm diameter bosses
  with real bores. Retained vertical pins sit in upper/lower fork plates.
- The loco carries a compact boxed drawgear housing and sliding shank. The rear
  fork has deep mounting cheeks seated on the moving unit's leading crossmember.
  The rigid link follows actual bogie yaw/heave/pitch; the short front shank
  slides to preserve pin seating. No new physics joint or engine behaviour.
- Native body material/atlas and the existing procedural mesh/ownership pattern
  are reused. The same owner removes the rear-mounted fork and all owned meshes
  on restoration. One model source file changes; the coupler-cache repair and
  low steam return remain unchanged.

The initial fitting eye grazed the native rear crossmember. Shortening the link
moved its front eye behind that face. Offline renders also exposed coplanar
boss/web and housing faces; their seams were corrected before the first runtime
build. James's subsequent screenshot required the upper bearer to be cut back,
which justified the second runtime build. Superseded fitting evidence and the
first DLL are retained separately and are not final acceptance evidence.

## Validation and handoff

Use `build/link-pin-0.3.1/validation.json` and the referenced exact-DLL reports.
The existing 27-pose native geometry fixture now checks the absence of the
upper bridge, both actual drawgear mount surfaces, eye/pin seating, bounded
shank travel, wheels/rods/pipe clearance, plate colliders and restoration.
Fixture source fitting is explicitly marked and cannot pass the package gate.
Final previews come from the exact staged runtime DLL, with that mode unset.

Final DLL SHA256:
`BBA33D317AE8A46BE7B0C36DAD4C8C9908C93630BEA17580F97CF7880EF5269A`.
All 16 native suites pass 28,906 checks; the private native ECS fixture passes
101 checks. Across the 27 geometry poses the maximum pin error is below 0.001 mm
and front shank travel is 45.73 mm from its straight pose. The old conventional
fixture assumed five meshes and classified all additional meshes as upper body;
it now identifies the four upper meshes by name, expects the new model parts
and checks rear-fork ownership. Only that fixture and the remaining two suites
were rerun; the earlier 13 exact-DLL results retain explicit provenance in the
aggregate report. No production rebuild was made for this fixture correction.

The clean build has zero warnings/errors. Semantic IL comparison protects all
371 existing types and 1,761 methods outside the bodywork owner, including the
previous coupler and steam-return fixes. The other donor bundles are reused
unchanged. Info.json must retain SHA256
`61F1A136148BAF8E18B3DE7339EC0D5943F7B13F5597435989D16645DA943DD4`.

No Derail Valley launch, game profile, gameplay automation, real installation,
push or publication. Offline fixture results do not establish game acceptance.
James owns the in-game visual/movement and teleport-away/return checks after a
separately authorised installation. The initial geometry remains a visual model
of the supported native single-car bogie joint, not a new drawgear simulation.
