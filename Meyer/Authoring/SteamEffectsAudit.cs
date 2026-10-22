using System;
using System.IO;
using System.Linq;
using CCL.Types.Components;
using CCL.Types.Proxies.Audio;
using CCL.Types.Proxies.VFX;
using UnityEngine;
using Object = UnityEngine.Object;

// Reopened prefab references, actual custom valve meshes and local-only native
// S060 cap geometry. Do not treat sampled transforms as live particle playback.
public static class SteamEffectsAudit
{
    public static void Validate(GameObject prefab, string output, bool meyer)
    {
        var clone = Object.Instantiate(prefab); int checks = 0; float max = 0;
        try
        {
            var frames = clone.GetComponentsInChildren<Transform>(true).Where(t => t.name == "bogie_car")
                .OrderBy(t => clone.transform.InverseTransformPoint(t.position).z).ToArray();
            Require(frames.Length == 2, "Exactly two powered effect frames required."); checks++;
            for (int unit = 0; unit < 2; unit++)
            {
                var frame = frames[unit];
                Require(frame.Find("[S060 fitted effects 0.7]") != null, "New cap-fitted effect contract marker missing."); checks++;
                var reader = frame.GetComponentsInChildren<CylinderCockParticlePortReaderProxy>(true).Single();
                var audio = frame.GetComponentsInChildren<CylinderCockLayeredPortReaderProxy>(true).Single();
                var controller = frame.GetComponentsInChildren<ParticlesPortReadersControllerProxy>(true).Single();
                string engine = (meyer ? unit == 1 : unit == 0) ? MeyerSteamAuthoring.Hp : MeyerSteamAuthoring.Lp;
                Require(reader.crankRotationPortId == engine + ".CRANK_ROTATION" && reader.cylindersInletValveOpenPortId == engine + ".CYLINDERS_INLET_VALVE_OPEN" &&
                    reader.cylinderCockFlowNormalizedPortId == engine + ".CYLINDER_COCK_FLOW_NORMALIZED", "Particle phase, valve mask and flow must belong to the same physical engine."); checks++;
                Require(audio.crankRotationPortId == reader.crankRotationPortId && audio.cylindersInletValveOpenPortId == reader.cylindersInletValveOpenPortId &&
                    audio.cylinderCockFlowNormalizedPortId == reader.cylinderCockFlowNormalizedPortId && audio.cylinderCockControlPortId == "cylinderCock.EXT_IN", "Native audio must use that engine and the existing cab control."); checks++;
                Require(reader.reverserPortId == "reverser.REVERSER" && reader.forwardSpeedPortId == "traction.FORWARD_SPEED_EXT_IN", "Native speed/reverser controls must remain unchanged."); checks++;
                foreach (float yaw in new[] { -15f, 0, 15f }) foreach (float bodyYaw in new[] { 0, 73f })
                {
                    clone.transform.SetPositionAndRotation(new Vector3(120, 20, -73), Quaternion.Euler(0, bodyYaw, 0));
                    frame.localRotation = Quaternion.Euler(0, yaw, 0);
                    for (int cylinder = 0; cylinder < 2; cylinder++)
                    {
                        int side = frame.InverseTransformPoint(audio.cylCockAudio[cylinder].transform.position).x < 0 ? -1 : 1;
                        bool native = meyer && unit == 1;
                        var front = SteamEffectsAuthoring.Outlet(frame, side, true, native);
                        var rear = SteamEffectsAuthoring.Outlet(frame, side, false, native);
                        var setup = reader.cylinderSetups[cylinder];
                        Near(setup.frontParticlesParent.transform.position, front);
                        Near(setup.rearParticlesParent.transform.position, rear);
                        Near(audio.cylCockAudio[cylinder].transform.position, (front + rear) / 2);
                        var drip = controller.particlePortReaders.Single(r => r.particlesParent.name == "Water drip " + cylinder);
                        Near(drip.particlesParent.transform.position, rear);
                        Require(Vector3.Dot(setup.frontParticlesParent.transform.forward, -frame.up) > .99999f &&
                            Vector3.Dot(setup.rearParticlesParent.transform.forward, -frame.up) > .99999f, "Cock steam must emit outwards below the valve cap."); checks++;
                        Require(drip.particleUpdaters.Any(u => u.portId == engine + ".WATER_IN_CYLINDERS_NORMALIZED") && drip.particleUpdaters.Any(u => u.portId == "cylinderCock.EXT_IN"), "Drips retain their unit's native water and shared cab gates."); checks++;
                        Require(audio.cylCockAudio[cylinder].GetComponents<CopyVanillaAudioSystem>().Single().AudioSystem == VanillaAudioSystem.SteamerCylinderCock, "Cock sound remains a live native alias."); checks++;
                    }
                }
                frame.localRotation = Quaternion.identity;
            }
            MeyerSteamAuthoring.ValidateOutletAliases(clone);
            File.WriteAllText(Path.Combine(output, "steam-effects-validation.json"), JsonUtility.ToJson(new Report {
                passed = true, checks = checks, maximumOutletGapMetres = max, actualDrainValveCaps = true,
                perUnitCrankValveFlowAndAudio = true, bogieYawAndBodyYaw = true, nativeAliasesOnly = true, gameRuntimeValidated = false
            }, true));
            void Near(Vector3 actual, Vector3 expected)
            { float gap = Vector3.Distance(actual, expected); max = Mathf.Max(max, gap); Require(gap < .0001f, "Effect holder detached from actual valve cap: " + gap); checks++; }
        }
        finally { Object.DestroyImmediate(clone); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    [Serializable] private sealed class Report
    {
        public bool passed, actualDrainValveCaps, perUnitCrankValveFlowAndAudio, bogieYawAndBodyYaw, nativeAliasesOnly, gameRuntimeValidated;
        public int checks;
        public float maximumOutletGapMetres;
    }
}
