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
        // Pooling can call Deinit again after enrollment has ended. These weak,
        // owner-free markers let us guard native readers' null-unsafe Deinit.
        private static readonly ConditionalWeakTable<ChuffClipsSimReader, object> ManagedAudio = new ConditionalWeakTable<ChuffClipsSimReader, object>();
        private static readonly ConditionalWeakTable<SteamSmokeParticlePortReader, object> ManagedSmoke = new ConditionalWeakTable<SteamSmokeParticlePortReader, object>();
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
                postfix: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(AudioInitPostfix)),
                finalizer: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(AudioInitFinalizer)));
            harmony.Patch(AccessTools.Method(typeof(ChuffClipsSimReader), nameof(ChuffClipsSimReader.Deinit)),
                prefix: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(AudioDeinitPrefix)),
                finalizer: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(AudioDeinitFinalizer)));
            harmony.Patch(AccessTools.Method(typeof(SteamSmokeParticlePortReader), nameof(SteamSmokeParticlePortReader.Init)),
                prefix: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(SmokeInitPrefix)),
                postfix: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(SmokeInitPostfix)),
                finalizer: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(SmokeInitFinalizer)));
            harmony.Patch(AccessTools.Method(typeof(SteamSmokeParticlePortReader), nameof(SteamSmokeParticlePortReader.Deinit)),
                prefix: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(SmokeDeinitPrefix)),
                finalizer: new HarmonyMethod(typeof(S060ExhaustPresentation), nameof(SmokeDeinitFinalizer)));
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
                Smoke.Add(reader, baseline); smoke.Add(baseline);
                ManagedSmoke.GetValue(reader, _ => new object());
                reader.Deinit(); reader.Init(Flow);
            }
            foreach (var reader in initializedAudio.Where(r => r != null).Distinct())
            {
                if (Audio.TryGetValue(reader, out _) || reader.chuffEventPort == null ||
                    reader.chuffEventPortId != S060EngineMapping.MainId + ".CHUFF_EVENT") continue;
                var baseline = new AudioBinding(this, reader);
                Audio.Add(reader, baseline); audio.Add(baseline);
                ManagedAudio.GetValue(reader, _ => new object());
                reader.Deinit(); reader.Init(Car, Flow);
            }
        }
        private static bool AudioInitPrefix(ChuffClipsSimReader __instance, TrainCar car, SimulationFlow simFlow, out AudioBinding __state)
        {
            __state = null;
            var owner = S060ArticulatedSimulation.ExhaustFor(simFlow);
            if (Audio.TryGetValue(__instance, out var current))
            {
                if (current.Owner == owner && current.Mode == owner.Bank.Mode && owner.Car == car) return false;
                __instance.Deinit();
            }
            if (owner == null || owner.Car != car || __instance.GetComponent<S060ExhaustCopy>() != null ||
                __instance.chuffEventPortId != S060EngineMapping.MainId + ".CHUFF_EVENT") return true;
            __state = new AudioBinding(owner, __instance);
            Audio.Add(__instance, __state); owner.audio.Add(__state);
            ManagedAudio.GetValue(__instance, _ => new object());
            __state.Prepare();
            return true;
        }
        private static void AudioInitPostfix(AudioBinding __state) => __state?.Complete();
        private static Exception AudioInitFinalizer(Exception __exception, AudioBinding __state)
            => __exception != null && __state != null ? EndAudio(__state, __exception, true) : __exception;
        private static bool AudioDeinitPrefix(ChuffClipsSimReader __instance)
            => !ManagedAudio.TryGetValue(__instance, out _) || !ReferenceEquals(__instance.car, null);
        private static Exception AudioDeinitFinalizer(ChuffClipsSimReader __instance, Exception __exception)
        {
            return Audio.TryGetValue(__instance, out var binding) ? EndAudio(binding, __exception, __exception != null) : __exception;
        }
        private static bool SmokeInitPrefix(SteamSmokeParticlePortReader __instance, SimulationFlow simFlow, out SmokeBinding __state)
        {
            __state = null;
            var owner = S060ArticulatedSimulation.ExhaustFor(simFlow);
            if (Smoke.TryGetValue(__instance, out var current))
            {
                if (current.Owner == owner && current.Mode == owner.Bank.Mode) return false;
                __instance.Deinit();
            }
            if (owner == null || __instance.chuffEventPortId != S060EngineMapping.MainId + ".CHUFF_EVENT") return true;
            __state = new SmokeBinding(owner, __instance);
            Smoke.Add(__instance, __state); owner.smoke.Add(__state);
            ManagedSmoke.GetValue(__instance, _ => new object());
            __state.Prepare();
            return true;
        }
        private static void SmokeInitPostfix(SmokeBinding __state) => __state?.Complete();
        private static Exception SmokeInitFinalizer(Exception __exception, SmokeBinding __state)
            => __exception != null && __state != null ? EndSmoke(__state, __exception) : __exception;
        private static bool SmokeDeinitPrefix(SteamSmokeParticlePortReader __instance)
            => !ManagedSmoke.TryGetValue(__instance, out _) ||
                (__instance.fireOnPort != null && __instance.chuffEventPort != null && __instance.isBoilerBrokenPort != null);
        private static Exception SmokeDeinitFinalizer(SteamSmokeParticlePortReader __instance, Exception __exception)
        {
            return Smoke.TryGetValue(__instance, out var binding) ? EndSmoke(binding, __exception) : __exception;
        }
        private static Exception EndAudio(AudioBinding binding, Exception failure, bool incompleteNative)
        {
            try { Cleanup(() => { if (incompleteNative) CleanupNativeAudio(binding.Reader, binding.Owner.Car); }, binding.End); }
            catch (Exception cleanup) { failure = Combine(failure, cleanup); }
            finally { binding.Owner.audio.Remove(binding); Audio.Remove(binding.Reader); }
            return failure;
        }
        private static Exception EndSmoke(SmokeBinding binding, Exception failure)
        {
            try { Cleanup(() => CleanupNativeSmoke(binding.Reader), binding.End); }
            catch (Exception cleanup) { failure = Combine(failure, cleanup); }
            finally { binding.Owner.smoke.Remove(binding); Smoke.Remove(binding.Reader); }
            return failure;
        }
        private static Exception Combine(Exception first, Exception next) => first == null ? next :
            new AggregateException("S060 exhaust lifecycle cleanup failed.", first, next);
        // Every independent teardown step runs, including authored-state restore,
        // even if a native/Unity cleanup operation itself fails.
        private static void Cleanup(params Action[] actions)
        {
            Exception failure = null;
            foreach (var action in actions)
                try { action(); } catch (Exception error) { failure = Combine(failure, error); }
            if (failure != null) throw failure;
        }
        private static Action<T> Native<T>(Type type, string name, object reader) =>
            AccessTools.MethodDelegate<Action<T>>(AccessTools.Method(type, name), reader);
        private static void CleanupNativeAudio(ChuffClipsSimReader reader, TrainCar fallback)
        {
            // B99 Init subscribes slide/slip before building audio configuration,
            // and movement/chuffs afterwards. Deinit assumes car is non-null;
            // this fallback must also handle a failure partway through Deinit.
            var car = ReferenceEquals(reader.car, null) ? fallback : reader.car;
            Cleanup(
                () => { if (!ReferenceEquals(car, null) && car.adhesionController != null)
                    car.adhesionController.WheelSlideStateChanged -= Native<bool>(typeof(ChuffClipsSimReader), "OnWheelSlideChange", reader); },
                () => { if (!ReferenceEquals(car, null) && car.adhesionController != null && car.adhesionController.wheelslipController.IsSome(out var slip))
                    slip.WheelslipStateChanged -= Native<bool>(typeof(ChuffClipsSimReader), "OnWheelslipChange", reader); },
                () => { if (!ReferenceEquals(car, null)) car.MovementStateChanged -= Native<bool>(typeof(ChuffClipsSimReader), "OnMovementStateChange", reader); },
                () => { if (reader.chuffEventPort != null) reader.chuffEventPort.ValueUpdatedInternally -= Native<float>(typeof(ChuffClipsSimReader), "OnChuff", reader); },
                () => reader.StopAllCoroutines(),
                () => { reader.car = null; reader.wheelSlideCoro = null; reader.wheelslipCoro = null;
                    reader.chuffEventPort = null; reader.exhaustPressurePort = null; reader.chuffFrequencyPort = null;
                    reader.cylinderWaterNormalizedPort = null; reader.cylinderCockControlPort = null; reader.ashesInPipesPort = null; });
        }
        private static void CleanupNativeSmoke(SteamSmokeParticlePortReader reader)
        {
            Cleanup(
                () => { if (reader.fireOnPort != null) reader.fireOnPort.ValueUpdatedInternally -= Native<float>(typeof(SteamSmokeParticlePortReader), "OnFireChanged", reader); },
                () => { if (reader.chuffEventPort != null) reader.chuffEventPort.ValueUpdatedInternally -= Native<float>(typeof(SteamSmokeParticlePortReader), "OnChuffEvent", reader); },
                () => { if (reader.isBoilerBrokenPort != null) reader.isBoilerBrokenPort.ValueUpdatedInternally -= Native<float>(typeof(SteamSmokeParticlePortReader), "OnBoilerBrokenChanged", reader); });
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
            private bool extraInitAttempted;
            private S060ShayExhaust shay;
            private readonly S060EngineMode mode;
            internal S060EngineMode Mode => mode;
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
                if (mode == S060EngineMode.Shay) shay = S060ShayExhaust.Apply(Reader);
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
                Extra = copy.GetComponent<ChuffClipsSimReader>();
                copy.name = "[S060 Dockside second exhaust]";
                copy.AddComponent<S060ExhaustCopy>();
                CopyAuthoredConfiguration(template, Extra);
                SetIds(Extra, ids.Select(id => Remap(id, S060EngineMapping.AdditionalExhaustId(mode))).ToArray());
                foreach (var id in Ids(Extra)) Require(Owner.Flow, id);
                // Native Init can throw after subscribing to car/adhesion events.
                // Enroll the attempt before calling it so End can always undo it.
                extraInitAttempted = true;
                Extra.Init(Owner.Car, Owner.Flow);
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
                var extra = Extra; var attempted = extraInitAttempted;
                Extra = null; extraInitAttempted = false;
                Cleanup(
                    () => { if (extra != null && attempted)
                        { try { if (!ReferenceEquals(extra.car, null)) extra.Deinit(); }
                          finally { CleanupNativeAudio(extra, Owner.Car); } } },
                    () => { if (extra != null) StopLoops(extra); },
                    () => { if (extra != null) extra.gameObject.SetActive(false); },
                    () => { if (extra != null) extra.transform.SetParent(null, false); },
                    () => { if (extra != null)
                        { if (Application.isPlaying) UnityEngine.Object.Destroy(extra.gameObject); else UnityEngine.Object.DestroyImmediate(extra.gameObject); } },
                    () => StopLoops(Reader),
                    () => { shay?.Dispose(); shay = null; },
                    () => { SetIds(Reader, ids); Reader.mediumPressureThreshold = medium; Reader.highPressureThreshold = high; });
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
                var actions = new List<Action>();
                foreach (var loops in new[] { reader.chuffLoops, reader.waterChuffLoops, reader.ashChuffLoops })
                    if (loops != null) foreach (var loop in loops)
                        if (loop?.chuffLoop != null)
                        { actions.Add(() => loop.chuffLoop.Stop()); actions.Add(() => loop.chuffLoop.Reset()); }
                foreach (var source in reader.GetComponentsInChildren<AudioSource>(true)) actions.Add(() => source.Stop());
                actions.Add(() => reader.anyLoopOngoing = false);
                Cleanup(actions.ToArray());
            }
        }

        internal sealed class SmokeBinding
        {
            internal readonly S060ExhaustPresentation Owner;
            internal readonly SteamSmokeParticlePortReader Reader;
            private readonly S060EngineMode mode;
            internal S060EngineMode Mode => mode;
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
                Require(Owner.Flow, Reader.fireOnPortId); Require(Owner.Flow, Reader.isBoilerBrokenPortId);
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
                var actions = new List<Action> {
                    () => { if (rearPulse != null && rearEvent != null) rearEvent.ValueUpdatedInternally -= rearPulse; },
                    () => { if (frontPulse != null && frontEvent != null) frontEvent.ValueUpdatedInternally -= frontPulse; },
                    () => { if (pressureChanged != null && rearPressure != null) rearPressure.ValueUpdatedInternally -= pressureChanged; },
                    () => { if (pressureChanged != null && frontPressure != null) frontPressure.ValueUpdatedInternally -= pressureChanged; },
                // Native Deinit only unsubscribes. Stop its old pressure-driven
                // coroutine and restore cached authored baselines before Init
                // enumerates leaves; closed/cold leaves may have been disabled.
                    () => Reader.StopAllCoroutines(),
                    () => { Reader.chuffCoro = null; Reader.stopAllCoro = null;
                        Reader.nextChuffQueued = false; Reader.allParticlesStopped = false; } };
                if (Reader.allParticlesSystemsData != null)
                    foreach (var data in Reader.allParticlesSystemsData)
                    {
                        if (data == null || data.ps == null) continue;
                        actions.Add(() => data.ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear));
                        actions.Add(() => { var main = data.ps.main; main.startSpeed = data.initialStartSpeed; main.maxParticles = data.initialMaxParticles;
                            var emission = data.ps.emission; emission.rateOverTime = data.initialRateOverTime; });
                        actions.Add(() => data.ps.gameObject.SetActive(true));
                    }
                actions.Add(() => { Reader.chuffEventPortId = eventId; Reader.exhaustPressurePortId = pressureId;
                    Owner.Flow.TryGetPort(eventId, out Reader.chuffEventPort); Owner.Flow.TryGetPort(pressureId, out Reader.exhaustPressurePort); });
                Cleanup(actions.ToArray());
            }
        }
    }
    internal sealed class S060ExhaustCopy : MonoBehaviour { }
}
#endif

