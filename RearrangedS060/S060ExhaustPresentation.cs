#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DV.ModularAudioCar;
using DV.Simulation.Controllers;
using HarmonyLib;
using LocoSim.Definitions;
using LocoSim.Implementations;
using UnityEngine;

namespace RearrangedS060
{
    // Native readers still own sound, mixing, smoke and coroutine timing. Only
    // their per-car exhaust sources change; no simulation/save ports are added.
    internal sealed class S060ExhaustPresentation
    {
        private static readonly ConditionalWeakTable<ChuffClipsSimReader, AudioBinding> Audio = new ConditionalWeakTable<ChuffClipsSimReader, AudioBinding>();
        private static readonly ConditionalWeakTable<SteamSmokeParticlePortReader, SmokeBinding> Smoke = new ConditionalWeakTable<SteamSmokeParticlePortReader, SmokeBinding>();
        private readonly List<AudioBinding> audio = new List<AudioBinding>();
        private readonly List<SmokeBinding> smoke = new List<SmokeBinding>();
        internal readonly TrainCar Car;
        internal readonly SimulationFlow Flow;
        internal readonly S060EngineBank Bank;
        internal S060ExhaustPresentation(TrainCar car, SimulationFlow flow, S060EngineBank bank)
        { Car = car; Flow = flow; Bank = bank; }

        internal static void Register(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ChuffClipsSimReader), nameof(ChuffClipsSimReader.Init)),
                prefix: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(AudioInitPrefix)),
                postfix: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(AudioInitPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ChuffClipsSimReader), nameof(ChuffClipsSimReader.Deinit)),
                postfix: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(AudioDeinitPostfix)));
            harmony.Patch(AccessTools.Method(typeof(SteamSmokeParticlePortReader), nameof(SteamSmokeParticlePortReader.Init)),
                prefix: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(SmokeInitPrefix)),
                postfix: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(SmokeInitPostfix)));
            harmony.Patch(AccessTools.Method(typeof(SteamSmokeParticlePortReader), nameof(SteamSmokeParticlePortReader.Deinit)),
                postfix: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(SmokeDeinitPostfix)));
        }
        internal void Refresh()
        {
            // Includes streamed/detached audio readers registered through Init.
            foreach (var binding in audio.ToArray())
                if (binding.Reader != null) { binding.Reader.Deinit(); binding.Reader.Init(Car, Flow); }
            foreach (var binding in smoke.ToArray())
                if (binding.Reader != null) { binding.Reader.Deinit(); binding.Reader.Init(Flow); }
        }
        internal void EnrollInitialized() => EnrollInitializedReaders(
            S060NativeAudio.Components<SteamSmokeParticlePortReader>(Car),
            S060NativeAudio.Components<ChuffClipsSimReader>(Car));

        // SimController initializes particles inside Initialize, before our
        // postfix publishes this owner. Rebind those readers once; later native
        // Init calls are enrolled by the existing hooks instead.
        internal void EnrollInitializedReaders(IEnumerable<SteamSmokeParticlePortReader> initializedSmoke,
            IEnumerable<ChuffClipsSimReader> initializedAudio)
        {
            foreach (var reader in initializedSmoke.Where(r => r != null).Distinct())
            {
                if (Smoke.TryGetValue(reader, out _) || reader.chuffEventPort == null ||
                    reader.chuffEventPortId != S060EngineMapping.MainId + ".CHUFF_EVENT") continue;
                var baseline = new SmokeBinding(this, reader);
                reader.Deinit(); baseline.End(); reader.Init(Flow);
            }
            foreach (var reader in initializedAudio.Where(r => r != null).Distinct())
            {
                if (Audio.TryGetValue(reader, out _) || reader.chuffEventPort == null ||
                    reader.chuffEventPortId != S060EngineMapping.MainId + ".CHUFF_EVENT") continue;
                reader.Deinit(); reader.Init(Car, Flow);
            }
        }
        private static void AudioInitPrefix(ChuffClipsSimReader __instance, TrainCar car, SimulationFlow simFlow)
        {
            var owner = S060ArticulatedSimulation.ExhaustFor(simFlow);
            if (owner == null || owner.Car != car || __instance.GetComponent<S060ExhaustCopy>() != null ||
                __instance.chuffEventPortId != S060EngineMapping.MainId + ".CHUFF_EVENT") return;
            var binding = new AudioBinding(owner, __instance);
            binding.Prepare(); Audio.Add(__instance, binding); owner.audio.Add(binding);
        }
        private static void AudioInitPostfix(ChuffClipsSimReader __instance)
        { if (Audio.TryGetValue(__instance, out var binding)) binding.Complete(); }
        private static void AudioDeinitPostfix(ChuffClipsSimReader __instance)
        {
            if (!Audio.TryGetValue(__instance, out var binding)) return;
            binding.End(); binding.Owner.audio.Remove(binding); Audio.Remove(__instance);
        }
        private static void SmokeInitPrefix(SteamSmokeParticlePortReader __instance, SimulationFlow simFlow)
        {
            var owner = S060ArticulatedSimulation.ExhaustFor(simFlow);
            if (owner == null || __instance.chuffEventPortId != S060EngineMapping.MainId + ".CHUFF_EVENT") return;
            var binding = new SmokeBinding(owner, __instance);
            binding.Prepare(); Smoke.Add(__instance, binding); owner.smoke.Add(binding);
        }
        private static void SmokeInitPostfix(SteamSmokeParticlePortReader __instance)
        { if (Smoke.TryGetValue(__instance, out var binding)) binding.Complete(); }
        private static void SmokeDeinitPostfix(SteamSmokeParticlePortReader __instance)
        {
            if (!Smoke.TryGetValue(__instance, out var binding)) return;
            binding.End(); binding.Owner.smoke.Remove(binding); Smoke.Remove(__instance);
        }
        private static string Remap(string value, string engine) => value?.Replace(S060EngineMapping.MainId + ".", engine + ".");
        private static Port Require(SimulationFlow flow, string id)
        {
            if (!flow.TryGetPort(id, out var port)) throw new InvalidOperationException("S060 exhaust port missing: " + id);
            return port;
        }

        internal sealed class AudioBinding
        {
            internal readonly S060ExhaustPresentation Owner;
            internal readonly ChuffClipsSimReader Reader;
            internal ChuffClipsSimReader Extra;
            private bool extraInitialized;
            private readonly S060EngineMode mode;
            private readonly string[] ids;
            private readonly float medium, high;
            internal AudioBinding(S060ExhaustPresentation owner, ChuffClipsSimReader reader)
            {
                Owner = owner; Reader = reader; mode = owner.Bank.Mode;
                ids = Ids(reader); medium = reader.mediumPressureThreshold; high = reader.highPressureThreshold;
            }
            internal void Prepare()
            {
                SetIds(Reader, ids.Select(id => Remap(id, S060EngineMapping.ExhaustId(mode))).ToArray());
                foreach (var id in Ids(Reader)) Require(Owner.Flow, id);
                // Verde's LP reader uses these lower pressure bands. Retain the
                // native volume curves, clips and loops rather than forcing gain.
                if (mode == S060EngineMode.Meyer) { Reader.mediumPressureThreshold = 3; Reader.highPressureThreshold = 4.5f; }
            }
            internal void Complete()
            {
                if (mode != S060EngineMode.Dockside) return;
                // Follow the S282 fork: copy the authored native audio subtree,
                // never a running loop whose pitch/volume are already modulated.
                var prefab = Owner.Car.carLivery?.parentType?.audioPrefab;
                var template = prefab != null ? prefab.GetComponentsInChildren<ChuffClipsSimReader>(true)
                    .SingleOrDefault(r => r.chuffEventPortId == ids[0]) : null;
                if (template == null) throw new InvalidOperationException("S060 authored chuff template missing.");
                var copy = UnityEngine.Object.Instantiate(template.gameObject, Reader.transform.parent);
                copy.name = "[S060 Dockside second exhaust]";
                copy.AddComponent<S060ExhaustCopy>();
                Extra = copy.GetComponent<ChuffClipsSimReader>();
                CopyAuthoredConfiguration(template, Extra);
                SetIds(Extra, ids.Select(id => Remap(id, S060EngineMapping.AdditionalExhaustId(mode))).ToArray());
                foreach (var id in Ids(Extra)) Require(Owner.Flow, id);
                Extra.Init(Owner.Car, Owner.Flow);
                extraInitialized = true;
            }
            private void CopyAuthoredConfiguration(ChuffClipsSimReader template, ChuffClipsSimReader copy)
            {
                // Construct fresh mutable loop/config objects explicitly. Clip
                // assets are immutable; runtime loop state must never be shared
                // with the authored prefab or the other engine's reader.
                copy.lowPressureClips = template.lowPressureClips;
                copy.mediumPressureClips = template.mediumPressureClips;
                copy.highPressureClips = template.highPressureClips;
                copy.waterChuffClips = template.waterChuffClips; copy.ashChuffClips = template.ashChuffClips;
                copy.pressureToVolumeCurve = Curve(template.pressureToVolumeCurve);
                copy.mediumPressureThreshold = template.mediumPressureThreshold; copy.highPressureThreshold = template.highPressureThreshold;
                copy.regularChuffConfig = Config(template.regularChuffConfig, Reader.regularChuffConfig.parent);
                copy.waterChuffConfig = Config(template.waterChuffConfig, Reader.waterChuffConfig.parent);
                copy.ashChuffConfig = Config(template.ashChuffConfig, Reader.ashChuffConfig.parent);
                copy.chuffLoops = Loops(template.chuffLoops); copy.waterChuffLoops = Loops(template.waterChuffLoops); copy.ashChuffLoops = Loops(template.ashChuffLoops);

                ChuffClipsSimReader.IndividualChuffAudioSourceConfig Config(ChuffClipsSimReader.IndividualChuffAudioSourceConfig source, Transform fallback)
                {
                    var parent = source.parent != null && source.parent.IsChildOf(template.transform)
                        ? Map(source.parent) : fallback;
                    return new ChuffClipsSimReader.IndividualChuffAudioSourceConfig { parent = parent,
                        chuffFrequencyToMasterVolume = Curve(source.chuffFrequencyToMasterVolume), pitch = source.pitch,
                        spatialCurve = Curve(source.spatialCurve), spread = source.spread, minDistance = source.minDistance,
                        maxDistance = source.maxDistance, mixerGroup = source.mixerGroup };
                }
                ChuffClipsSimReader.ChuffLoop[] Loops(ChuffClipsSimReader.ChuffLoop[] source) => source.Select(loop =>
                    new ChuffClipsSimReader.ChuffLoop { chuffLoop = Map(loop.chuffLoop.transform).GetComponent<LayeredAudio>(),
                        chuffFrequencyToMasterVolume = Curve(loop.chuffFrequencyToMasterVolume) }).ToArray();
                Transform Map(Transform source)
                {
                    var path = new Stack<string>();
                    for (var node = source; node != template.transform; node = node.parent)
                    {
                        if (node == null) throw new InvalidOperationException("Native S060 chuff loop is outside its authored subtree.");
                        path.Push(node.name);
                    }
                    return path.Count == 0 ? copy.transform : copy.transform.Find(string.Join("/", path));
                }
            }
            private static AnimationCurve Curve(AnimationCurve source) => new AnimationCurve(source.keys)
                { preWrapMode = source.preWrapMode, postWrapMode = source.postWrapMode };
            internal void End()
            {
                if (Extra != null)
                {
                    if (extraInitialized) { Extra.Deinit(); StopLoops(Extra); }
                    foreach (var source in Extra.GetComponentsInChildren<AudioSource>(true)) source.Stop();
                    Extra.gameObject.SetActive(false); Extra.transform.SetParent(null, false);
                    UnityEngine.Object.Destroy(Extra.gameObject); Extra = null;
                    extraInitialized = false;
                }
                StopLoops(Reader);
                SetIds(Reader, ids); Reader.mediumPressureThreshold = medium; Reader.highPressureThreshold = high;
            }
            private static string[] Ids(ChuffClipsSimReader reader) => new[] { reader.chuffEventPortId,
                reader.exhaustPressurePortId, reader.chuffFrequencyPortId, reader.cylinderWaterNormalizedPortId,
                reader.cylinderCockControlPortId, reader.ashesInPipesPortId };
            private static void SetIds(ChuffClipsSimReader reader, string[] ids)
            {
                reader.chuffEventPortId = ids[0]; reader.exhaustPressurePortId = ids[1]; reader.chuffFrequencyPortId = ids[2];
                reader.cylinderWaterNormalizedPortId = ids[3]; reader.cylinderCockControlPortId = ids[4]; reader.ashesInPipesPortId = ids[5];
            }
            private static void StopLoops(ChuffClipsSimReader reader)
            {
                foreach (var loop in reader.chuffLoops.Concat(reader.waterChuffLoops).Concat(reader.ashChuffLoops))
                    if (loop.chuffLoop != null) { loop.chuffLoop.Stop(); loop.chuffLoop.Reset(); }
                foreach (var source in reader.GetComponentsInChildren<AudioSource>(true)) source.Stop();
            }
        }

        internal sealed class SmokeBinding
        {
            internal readonly S060ExhaustPresentation Owner;
            internal readonly SteamSmokeParticlePortReader Reader;
            private readonly S060EngineMode mode;
            private readonly string eventId, pressureId;
            private Port rearEvent, frontEvent, rearPressure, frontPressure;
            private Action<float> rearPulse, frontPulse, pressureChanged;
            internal Port CombinedPressure;
            internal SmokeBinding(S060ExhaustPresentation owner, SteamSmokeParticlePortReader reader)
            {
                Owner = owner; Reader = reader; mode = owner.Bank.Mode;
                eventId = reader.chuffEventPortId; pressureId = reader.exhaustPressurePortId;
            }
            internal void Prepare()
            {
                Reader.chuffEventPortId = Remap(eventId, S060EngineMapping.ExhaustId(mode));
                Reader.exhaustPressurePortId = Remap(pressureId, S060EngineMapping.ExhaustId(mode));
                Require(Owner.Flow, Reader.chuffEventPortId); Require(Owner.Flow, Reader.exhaustPressurePortId);
            }
            internal void Complete()
            {
                if (mode != S060EngineMode.Dockside) return;
                rearEvent = Require(Owner.Flow, eventId);
                frontEvent = Require(Owner.Flow, Remap(eventId, S060EngineMapping.AdditionalExhaustId(mode)));
                rearPressure = Require(Owner.Flow, pressureId);
                frontPressure = Require(Owner.Flow, Remap(pressureId, S060EngineMapping.AdditionalExhaustId(mode)));
                var pulse = AccessTools.MethodDelegate<Action<float>>(AccessTools.Method(typeof(SteamSmokeParticlePortReader), "OnChuffEvent"), Reader);
                // One native chimney and one coroutine owner. Forward each event
                // directly (equal event indices from different units must not be
                // collapsed by a synthetic Port's change-only notification).
                rearEvent.ValueUpdatedInternally -= pulse;
                CombinedPressure = new Port("rs060Presentation", new PortDefinition(PortType.READONLY_OUT, PortValueType.PRESSURE, "CHIMNEY_PRESSURE"), 1);
                pressureChanged = _ => CombinedPressure.Value = Mathf.Max(1, Mathf.Max(rearPressure.Value, frontPressure.Value));
                rearPressure.ValueUpdatedInternally += pressureChanged; frontPressure.ValueUpdatedInternally += pressureChanged;
                pressureChanged(0); Reader.exhaustPressurePort = CombinedPressure;
                rearPulse = index => { if (rearPressure.Value > 1) pulse(index); };
                frontPulse = index => { if (frontPressure.Value > 1) pulse(index); };
                rearEvent.ValueUpdatedInternally += rearPulse; frontEvent.ValueUpdatedInternally += frontPulse;
            }
            internal void End()
            {
                if (rearPulse != null) rearEvent.ValueUpdatedInternally -= rearPulse;
                if (frontPulse != null) frontEvent.ValueUpdatedInternally -= frontPulse;
                if (pressureChanged != null)
                { rearPressure.ValueUpdatedInternally -= pressureChanged; frontPressure.ValueUpdatedInternally -= pressureChanged; }
                // Native Deinit only unsubscribes. Stop its old pressure-driven
                // coroutine and restore cached authored baselines before Init
                // enumerates leaves; closed/cold leaves may have been disabled.
                Reader.StopAllCoroutines(); Reader.chuffCoro = null; Reader.stopAllCoro = null;
                Reader.nextChuffQueued = false; Reader.allParticlesStopped = false;
                if (Reader.allParticlesSystemsData != null)
                    foreach (var data in Reader.allParticlesSystemsData)
                    {
                        if (data.ps == null) continue;
                        data.ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        var main = data.ps.main; main.startSpeed = data.initialStartSpeed; main.maxParticles = data.initialMaxParticles;
                        var emission = data.ps.emission; emission.rateOverTime = data.initialRateOverTime;
                        data.ps.gameObject.SetActive(true);
                    }
                Reader.chuffEventPortId = eventId; Reader.exhaustPressurePortId = pressureId;
                Reader.chuffEventPort = Require(Owner.Flow, eventId); Reader.exhaustPressurePort = Require(Owner.Flow, pressureId);
            }
        }
    }
    internal sealed class S060ExhaustCopy : MonoBehaviour { }
}
#endif

