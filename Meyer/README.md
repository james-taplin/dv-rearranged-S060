# Rearranged S060 Meyer — 0.6.0 test pack

Requires RearrangedS060 0.13.0 and Custom Car Loader. Select **Meyer Compound**
from the native S060's WHEEL REARRANGER radio or UMM arrangement chooser.

The arrangement keeps one S060 identity, cab, fuel, water, damage and save. Two
powered axles sit beneath the locomotive and two beneath its attached rear
pannier section. No companion vehicle is spawned. Panniers add no water capacity.
Stop, close the regulator, uncouple the rear and disconnect its hose before
adding/removing Meyer; leave clear track behind. Stock restores the original
rear coupling position, supports, length and meshes.

The front uses the complete original S060 cylinder renderers at full size.
Both engines' cocks share the native cab lever. Serial boiler -> HP -> finite
receiver -> LP -> blast-pipe simulation and dormant state remain unchanged.
With runtime 0.13.1, chimney pulses and regular chuffs use the LP exhaust and
lower-pressure sound bands. discordraconequus's Verde compound provided the reference;
no Verde meshes or audio are included. See the core package's CREDITS.md.
Both engines drive the existing native traction system; slip depends on actual
steam pressure, controls, braking and track grip.

This pack adds a sloped bunker top, connected rear fittings, shortened visual
rods/guides, and pipe joins checked against actual mesh socket faces. The two
lower rear lamps, glass and native controlled effects move to the rear section;
the upper lamp remains on the cab. Native rear buffer compression is retained.
The original frame crossmember remains. Native game meshes/materials stay live
references, not bundled art.

Each driven axle has two functional big-end oil cups; the four original guide/
crosshead lubricators remain. Oil quantity and lid state survive switching and
save/load; dormant cups retain their state. Use the native oil can and controls.
The CCL standalone prototype remains a debug asset donor and uses its authored
front-cylinder fallback; use the native S060 radio for the production layout.

Offline native simulation, geometry and reopened-bundle tests pass; the complete
new game build still needs visual, oil-can, lamp-control and save/reload tests.
The included runtime captures bounded read-only powered diagnostics automatically.
