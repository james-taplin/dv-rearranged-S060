#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DV.ModularAudioCar;
using DV.Simulation.Controllers;
using HarmonyLib;
using LocoSim.Implementations;
using UnityEngine;

namespace RearrangedS282
{
    // Native readers own timing, mixing, cylinder masks and particle updates.
    // This bridge owns only their extra rear-engine instances and leaf placement.
    internal static class ArticulatedEffects
    {
        private const float EngineSeparation = 4.65f;
        private static readonly ConditionalWeakTable<AudioClipSimReadersController, ChuffBinding> Chuffs = new();
        private static readonly ConditionalWeakTable<LayeredAudioSimReadersController, CockBinding> Cocks = new();
        private static readonly System.Reflection.FieldInfo ParticleFlow = AccessTools.Field(typeof(ParticlesPortReadersController), "simFlow");
        private static readonly System.Reflection.MethodInfo SmokeChuff = AccessTools.Method(typeof(SteamSmokeParticlePortReader), "OnChuffEvent");

        internal static void Register(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(ClipInit)).Patch();
            harmony.CreateClassProcessor(typeof(ClipDeinit)).Patch();
            harmony.CreateClassProcessor(typeof(LayerInit)).Patch();
            harmony.CreateClassProcessor(typeof(LayerDeinit)).Patch();
            harmony.CreateClassProcessor(typeof(ParticleInit)).Patch();
            harmony.CreateClassProcessor(typeof(ParticleDestroy)).Patch();
        }

        internal static void Refresh(TrainCar car, bool active)
        {
            if (car == null) return;
            var state = car.GetComponent<EffectsState>();
            if (!active) { if (state != null) state.Stop(); return; }
            state ??= car.gameObject.AddComponent<EffectsState>();
            state.Car = car;
            state.Active = true;
            state.RefreshNow();
        }

        internal static void Deinit(TrainCar car) { if (car != null) car.GetComponent<EffectsState>()?.Stop(); }

        internal static bool IsOwnedPresentation(Transform t)
        {
            for (; t != null; t = t.parent) if (t.GetComponent<ArticulatedEffectMarker>() != null) return true;
            return false;
        }

        private static EffectsState ActiveState(TrainCar car)
        {
            var state = car != null ? car.GetComponent<EffectsState>() : null;
            return state != null && state.Active ? state : null;
        }

        private static void Safe(Action action)
        {
            try { action(); }
            catch (Exception ex) { try { Main.Logger.Warning("Articulated effects cleanup: " + ex.Message); } catch { } }
        }

        private static void RequirePorts(SimulationFlow flow, params string[] ids)
        {
            foreach (var id in ids)
                if (!flow.TryGetPort(id, out _)) throw new InvalidOperationException("Articulated effects port missing: " + id);
        }

        private static IEnumerable<T> AudioControllers<T>(TrainCar car) where T : Component
        {
            // Native TrainCar.SetupInterior makes this a separate scene root.
            // Include it explicitly, retaining car descendants for other layouts.
            var seen = new HashSet<T>();
            foreach (var controller in car.GetComponentsInChildren<T>(true))
                if (controller != null && seen.Add(controller)) yield return controller;
            if (car.interior != null)
                foreach (var controller in car.interior.GetComponentsInChildren<T>(true))
                    if (controller != null && seen.Add(controller)) yield return controller;
        }

        private static bool AudioBindingMatches(AudioBinding binding, TrainCar car, SimulationFlow flow)
        {
            return binding.Ready && ReferenceEquals(binding.Car, car) && ReferenceEquals(binding.Flow, flow);
        }

        private static GameObject Copy(GameObject source, Transform parent, string name)
        {
            var copy = UnityEngine.Object.Instantiate(source, parent);
            copy.name = "[Rearranged articulated effects] " + name;
            copy.AddComponent<ArticulatedEffectMarker>();
            foreach (var audio in copy.GetComponentsInChildren<AudioSource>(true)) audio.Stop();
            return copy;
        }

        private static void DestroyCopy(GameObject copy)
        {
            if (copy == null) return;
            foreach (var audio in copy.GetComponentsInChildren<AudioSource>(true)) Safe(audio.Stop);
            foreach (var particle in copy.GetComponentsInChildren<ParticleSystem>(true))
                Safe(() => particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear));
            copy.SetActive(false);
            copy.transform.SetParent(null, true);
            UnityEngine.Object.Destroy(copy);
        }

        private static T Template<T>(TrainCar car, Func<T, bool> predicate) where T : Component
        {
            // A live loop source's pitch/clip may already be adapted. Clone the
            // authored native audio prefab so that transient playback state is
            // never mistaken for the new reader's baseline.
            var prefab = car.carLivery?.parentType?.audioPrefab;
            return prefab != null ? prefab.GetComponentsInChildren<T>(true).FirstOrDefault(predicate) : null;
        }

        private sealed class Pose
        {
            internal readonly Transform Leaf, Parent;
            private readonly Vector3 position, scale;
            private readonly Quaternion rotation;
            internal Pose(Transform leaf)
            { Leaf = leaf; Parent = leaf.parent; position = leaf.localPosition; rotation = leaf.localRotation; scale = leaf.localScale; }
            internal void Restore()
            {
                if (Leaf == null) return;
                Leaf.SetParent(Parent, false); Leaf.localPosition = position; Leaf.localRotation = rotation; Leaf.localScale = scale;
            }
        }

        internal abstract class AudioBinding
        {
            internal TrainCar Car;
            internal SimulationFlow Flow;
            internal bool Ready, Initialized;
            internal GameObject CopyRoot;
            internal EffectsState Owner;
            internal abstract void Remove();
            protected void Forget()
            {
                DestroyCopy(CopyRoot); CopyRoot = null;
                if (Owner != null) Owner.Audio.Remove(this);
                Owner = null; Initialized = false;
            }
        }

        private sealed class ChuffBinding : AudioBinding
        {
            internal AudioClipSimReadersController Controller;
            internal ChuffClipsSimReader Reader;
            internal void Add(EffectsState owner)
            {
                if (CopyRoot != null || !Ready) return;
                var engine = owner.Simulation.RearEngine;
                var template = Template<ChuffClipsSimReader>(Car, r => r.chuffEventPortId == owner.Simulation.MainEngine.chuffEventReadOut.id);
                if (template == null) throw new InvalidOperationException("Native S282 chuff template missing.");
                Owner = owner; owner.Audio.Add(this);
                CopyRoot = Copy(template.gameObject, Controller.transform, "rear exhaust");
                Reader = CopyRoot.GetComponent<ChuffClipsSimReader>();
                Reader.chuffEventPortId = engine.chuffEventReadOut.id;
                Reader.exhaustPressurePortId = engine.exhaustPressureReadOut.id;
                Reader.chuffFrequencyPortId = engine.chuffFrequencyReadOut.id;
                Reader.cylinderWaterNormalizedPortId = engine.waterInCylindersNormalizedReadOut.id;
                Reader.ashesInPipesPortId = engine.ashesInPipesReadOut.id;
                RequirePorts(Flow, Reader.chuffEventPortId, Reader.exhaustPressurePortId, Reader.chuffFrequencyPortId,
                    Reader.cylinderWaterNormalizedPortId, Reader.cylinderCockControlPortId, Reader.ashesInPipesPortId);
                Controller.entries = Controller.entries.Append(Reader).ToArray();
                Initialized = true; Reader.Init(Car, Flow);
            }
            internal override void Remove()
            {
                if (Reader != null && Initialized) Safe(Reader.Deinit);
                if (Controller != null && Reader != null) Controller.entries = Controller.entries.Where(r => r != Reader).ToArray();
                Reader = null; Forget();
            }
        }

        private sealed class CockBinding : AudioBinding
        {
            internal LayeredAudioSimReadersController Controller;
            internal CylinderCockLayeredPortReader Reader;
            private readonly List<Pose> primaryLeaves = new();
            private readonly List<GameObject> secondaryLeaves = new();
            internal void Add(EffectsState owner)
            {
                if (CopyRoot != null || !Ready) return;
                var primary = Controller.entries.OfType<CylinderCockLayeredPortReader>()
                    .FirstOrDefault(r => !IsOwnedPresentation(r.transform) && r.crankRotationPortId == owner.Simulation.MainEngine.crankRotationReadOut.id);
                var template = Template<CylinderCockLayeredPortReader>(Car, r => r.crankRotationPortId == owner.Simulation.MainEngine.crankRotationReadOut.id);
                if (primary == null || template == null) throw new InvalidOperationException("Native S282 cylinder-cock audio template missing.");
                Owner = owner; owner.Audio.Add(this);
                CopyRoot = Copy(template.gameObject, Controller.transform, "rear cylinder-cock audio");
                Reader = CopyRoot.GetComponent<CylinderCockLayeredPortReader>();
                var engine = owner.Simulation.RearEngine;
                Reader.crankRotationPortId = engine.crankRotationReadOut.id;
                Reader.cylindersInletValveOpenPortId = engine.cylindersInletValveOpen.id;
                Reader.cylinderCockFlowNormalizedPortId = engine.cylinderCockFlowNormalizedReadOut.id;
                RequirePorts(Flow, Reader.crankRotationPortId, Reader.cylindersInletValveOpenPortId,
                    Reader.cylinderCockFlowNormalizedPortId, Reader.cylinderCockControlPortId);
                foreach (var audio in primary.cylCockAudio)
                {
                    if (audio == null) continue;
                    primaryLeaves.Add(new Pose(audio.transform));
                    Vector3 world = audio.transform.position;
                    audio.transform.SetParent(owner.Front, true);
                    audio.transform.localPosition = owner.FrontOutletLocal(world, audio.transform.localPosition);
                }
                foreach (var audio in Reader.cylCockAudio)
                {
                    if (audio == null) continue;
                    // Templates live in audio-prefab coordinates; retain their
                    // authored car-local outlet coordinates in the live car.
                    Vector3 carPosition = template.transform.root.InverseTransformPoint(template.cylCockAudio[Array.IndexOf(Reader.cylCockAudio, audio)].transform.position);
                    audio.transform.SetParent(owner.Rear, false);
                    audio.transform.localPosition = owner.RearOutletLocal(carPosition);
                    audio.gameObject.AddComponent<ArticulatedEffectMarker>(); secondaryLeaves.Add(audio.gameObject);
                }
                Controller.entries = Controller.entries.Append(Reader).ToArray();
                Initialized = true; Reader.Init(Car, Flow);
            }
            internal override void Remove()
            {
                if (Reader != null && Initialized) Safe(Reader.Deinit);
                if (Controller != null && Reader != null) Controller.entries = Controller.entries.Where(r => r != Reader).ToArray();
                foreach (var pose in primaryLeaves) Safe(pose.Restore); primaryLeaves.Clear();
                foreach (var leaf in secondaryLeaves) Safe(() => DestroyCopy(leaf)); secondaryLeaves.Clear();
                Reader = null; Forget();
            }
        }

        internal sealed class EffectsState : MonoBehaviour
        {
            internal TrainCar Car;
            internal bool Active;
            internal readonly HashSet<AudioBinding> Audio = new();
            internal ArticulatedSimulation Simulation => Car.GetComponent<ArticulatedSimulation>();
            internal Transform Front => Car.GetComponent<WheelRearranger>().ArticulatedFrontRoot;
            internal Transform Rear => Car.GetComponent<WheelRearranger>().ArticulatedRearRoot;
            internal Vector3 FrontOutletOffset => Car.GetComponent<WheelRearranger>().currentWA == WheelArrangementType.s4444
                ? Vector3.forward * .35f : Vector3.zero;
            // Front outlet in front-engine coordinates, from its native world position.
            internal Vector3 FrontOutletLocal(Vector3 nativeWorld, Vector3 reparentedLocal) =>
                Car.GetComponent<WheelRearranger>().TryMapArticulatedOutlet(nativeWorld, false, out var mapped)
                    ? mapped : reparentedLocal + FrontOutletOffset;
            // Rear outlet in rear-engine coordinates, from its authored car-local position.
            internal Vector3 RearOutletLocal(Vector3 carLocal) =>
                Car.GetComponent<WheelRearranger>().TryMapArticulatedOutlet(Car.transform.TransformPoint(carLocal), true, out var mapped)
                    ? mapped : carLocal + Vector3.back * EngineSeparation;
            private ParticleBinding particles;
            private float nextSearch;
            private bool warned;

            internal void RefreshNow()
            {
                if (!Active || Car == null || Simulation == null || !Simulation.IsActive || Front == null || Rear == null) return;
                try
                {
                    foreach (var controller in AudioControllers<AudioClipSimReadersController>(Car))
                        if (Chuffs.TryGetValue(controller, out var binding) && AudioBindingMatches(binding, Car, Simulation.Flow)) binding.Add(this);
                    foreach (var controller in AudioControllers<LayeredAudioSimReadersController>(Car))
                        if (Cocks.TryGetValue(controller, out var binding) && AudioBindingMatches(binding, Car, Simulation.Flow)) binding.Add(this);
                    if (particles == null)
                    {
                        var controller = Car.SimController.particlesController;
                        if (controller != null && ReferenceEquals(ParticleFlow?.GetValue(controller), Simulation.Flow))
                        { particles = new ParticleBinding(this, controller); particles.Add(); }
                    }
                }
                catch (Exception ex)
                {
                    if (!warned) { Main.Logger.LogException("Articulated native effects setup failed", ex); warned = true; }
                    Stop();
                }
            }
            internal void Stop()
            {
                Active = false;
                foreach (var binding in Audio.ToArray()) Safe(binding.Remove);
                Audio.Clear();
                if (particles != null) Safe(particles.Remove);
                particles = null;
            }
            internal void RemoveParticles(ParticlesPortReadersController controller)
            {
                if (particles == null || !particles.BelongsTo(controller)) return;
                Safe(particles.Remove); particles = null;
            }
            private void LateUpdate()
            {
                if (Active && Time.unscaledTime >= nextSearch) { nextSearch = Time.unscaledTime + .5f; RefreshNow(); }
            }
            private void OnDestroy() { Stop(); }
        }

        private sealed class ParticleBinding
        {
            private readonly EffectsState owner;
            private readonly ParticlesPortReadersController controller;
            private readonly SimulationFlow flow;
            private readonly List<Pose> primaryLeaves = new();
            private readonly List<GameObject> copies = new();
            private readonly List<ParticlesPortReadersController.ParticlePortReader> engineOutlets = new();
            private CylinderCockParticlePortReader reader;
            private bool initialized;
            private Port smokeEvents;
            private Action<float> smokeCallback;

            internal ParticleBinding(EffectsState owner, ParticlesPortReadersController controller)
            { this.owner = owner; this.controller = controller; flow = owner.Simulation.Flow; }
            internal bool BelongsTo(ParticlesPortReadersController other) => controller == other;

            internal void Add()
            {
                var main = owner.Simulation.MainEngine;
                var rear = owner.Simulation.RearEngine;
                var original = controller.entries.OfType<CylinderCockParticlePortReader>()
                    .FirstOrDefault(r => !IsOwnedPresentation(r.transform) && r.crankRotationPortId == main.crankRotationReadOut.id);
                var prefab = owner.Car.carLivery.prefab;
                var templateController = prefab != null ? prefab.GetComponentsInChildren<ParticlesPortReadersController>(true)
                    .FirstOrDefault(c => c.entries.OfType<CylinderCockParticlePortReader>().Any(r => r.crankRotationPortId == main.crankRotationReadOut.id)) : null;
                var template = templateController != null ? templateController.entries.OfType<CylinderCockParticlePortReader>()
                    .FirstOrDefault(r => r.crankRotationPortId == main.crankRotationReadOut.id) : null;
                if (original == null || template == null) throw new InvalidOperationException("Native cylinder-cock particle template missing.");
                RequirePorts(flow, rear.crankRotationReadOut.id, template.reverserPortId,
                    rear.cylindersInletValveOpen.id, rear.cylinderCockFlowNormalizedReadOut.id, template.forwardSpeedPortId);
                var outletPorts = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { main.waterInCylindersNormalizedReadOut.id, rear.waterInCylindersNormalizedReadOut.id },
                    { main.cylinderCrackFlowNormalizedReadOut.id, rear.cylinderCrackFlowNormalizedReadOut.id }
                };
                // Verify every authored outlet and its complete port list before
                // moving native leaves or registering any extra particle reader.
                var outletDefinitions = controller.particlePortReaders
                    .Where(source => source.particlesParent != null && source.particleUpdaters.Any(p => outletPorts.ContainsKey(p.portId)))
                    .Select(source =>
                    {
                        var leaf = templateController.transform.Find(LocalPath(source.particlesParent.transform, controller.transform));
                        if (leaf == null) throw new InvalidOperationException("Native cylinder outlet template missing.");
                        var updaters = source.particleUpdaters.Select(p => new ParticlesPortReadersController.ParticlePortReader.PortParticleUpdateDefinition
                        {
                            portId = outletPorts.TryGetValue(p.portId, out var id) ? id : p.portId,
                            inputModifier = p.inputModifier,
                            propertiesToUpdate = p.propertiesToUpdate
                        }).ToList();
                        RequirePorts(flow, updaters.Select(p => p.portId).ToArray());
                        return new { Source = source, Template = leaf, Updaters = updaters };
                    }).ToArray();
                // A live primary's speed/size/emission may already be modulated.
                // The rear reader must capture authored particle baselines.
                var root = Copy(template.gameObject, controller.transform, "rear cylinder-cock particles"); copies.Add(root);
                root.transform.position = original.transform.position; root.transform.rotation = original.transform.rotation;
                reader = root.GetComponent<CylinderCockParticlePortReader>();
                reader.crankRotationPortId = rear.crankRotationReadOut.id;
                reader.cylindersInletValveOpenPortId = rear.cylindersInletValveOpen.id;
                reader.cylinderCockFlowNormalizedPortId = rear.cylinderCockFlowNormalizedReadOut.id;
                RequirePorts(flow, reader.crankRotationPortId, reader.reverserPortId,
                    reader.cylindersInletValveOpenPortId, reader.cylinderCockFlowNormalizedPortId, reader.forwardSpeedPortId);
                foreach (var setup in original.cylinderSetups)
                    foreach (var leaf in new[] { setup.frontParticlesParent, setup.rearParticlesParent }) MovePrimary(leaf);
                foreach (var setup in reader.cylinderSetups)
                    foreach (var leaf in new[] { setup.frontParticlesParent, setup.rearParticlesParent }) MoveCopy(leaf);
                controller.entries = controller.entries.Append(reader).ToArray();
                initialized = true; reader.Init(flow);
                foreach (var definition in outletDefinitions)
                {
                    var source = definition.Source;
                    bool crack = source.particleUpdaters.Any(p => p.portId == main.cylinderCrackFlowNormalizedReadOut.id);
                    var parent = Copy(definition.Template.gameObject, controller.transform, crack ? "rear cylinder crack" : "rear cylinder water drip"); copies.Add(parent);
                    parent.transform.position = source.particlesParent.transform.position; parent.transform.rotation = source.particlesParent.transform.rotation;
                    MovePrimary(source.particlesParent); MoveCopy(parent);
                    var outlet = new ParticlesPortReadersController.ParticlePortReader
                    {
                        particlesParent = parent,
                        particlesPortReader = controller,
                        particleUpdaters = definition.Updaters
                    };
                    engineOutlets.Add(outlet); controller.particlePortReaders.Add(outlet); outlet.Init();
                    foreach (var updater in outlet.particleUpdaters) updater.Init(outlet, flow);
                }
                var smoke = controller.entries.OfType<SteamSmokeParticlePortReader>().FirstOrDefault(r => r.chuffEventPortId == main.chuffEventReadOut.id);
                if (smoke != null && SmokeChuff != null)
                {
                    var pulse = AccessTools.MethodDelegate<Action<float>>(SmokeChuff, smoke);
                    smokeEvents = rear.chuffEventReadOut;
                    // Both exhaust groups share one native chimney. Retain its
                    // main pressure response; gate the extra pulse by rear
                    // pressure. Never add numeric event indices or duplicate fire.
                    smokeCallback = value => { if (rear.exhaustPressureReadOut.Value > 1f) pulse(value); };
                    smokeEvents.ValueUpdatedInternally += smokeCallback;
                }
            }

            private static string LocalPath(Transform leaf, Transform root)
            {
                var names = new Stack<string>();
                for (var current = leaf; current != root; current = current.parent)
                {
                    if (current == null) throw new InvalidOperationException("Native particle outlet is outside its controller.");
                    names.Push(current.name);
                }
                return string.Join("/", names);
            }

            private void MovePrimary(GameObject leaf)
            {
                if (leaf == null || primaryLeaves.Any(p => p.Leaf == leaf.transform)) return;
                primaryLeaves.Add(new Pose(leaf.transform));
                Vector3 world = leaf.transform.position;
                leaf.transform.SetParent(owner.Front, true);
                leaf.transform.localPosition = owner.FrontOutletLocal(world, leaf.transform.localPosition);
            }

            private void MoveCopy(GameObject leaf)
            {
                if (leaf == null) return;
                Vector3 position = owner.Car.transform.InverseTransformPoint(leaf.transform.position);
                Quaternion rotation = leaf.transform.rotation;
                leaf.transform.SetParent(owner.Rear, false);
                leaf.transform.localPosition = owner.RearOutletLocal(position);
                leaf.transform.rotation = rotation;
                if (leaf.GetComponent<ArticulatedEffectMarker>() == null) leaf.AddComponent<ArticulatedEffectMarker>();
                if (!copies.Contains(leaf)) copies.Add(leaf);
                foreach (var particle in leaf.GetComponentsInChildren<ParticleSystem>(true))
                {
                    for (var t = particle.transform; t != null && t != leaf.transform.parent; t = t.parent) t.gameObject.SetActive(true);
                    var settings = particle.main; settings.playOnAwake = false;
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            internal void Remove()
            {
                if (smokeEvents != null && smokeCallback != null) smokeEvents.ValueUpdatedInternally -= smokeCallback;
                smokeEvents = null; smokeCallback = null;
                if (reader != null && initialized) Safe(reader.Deinit);
                if (reader != null && controller != null) controller.entries = controller.entries.Where(r => r != reader).ToArray();
                foreach (var outlet in engineOutlets)
                {
                    foreach (var updater in outlet.particleUpdaters) Safe(() => updater.Deinit(flow));
                    Safe(outlet.OnDisable); if (controller != null) controller.particlePortReaders.Remove(outlet);
                }
                engineOutlets.Clear();
                foreach (var pose in primaryLeaves) Safe(pose.Restore); primaryLeaves.Clear();
                foreach (var copy in copies) Safe(() => DestroyCopy(copy)); copies.Clear();
                reader = null; initialized = false;
            }
        }

        [HarmonyPatch(typeof(AudioClipSimReadersController), nameof(AudioClipSimReadersController.Init))]
        private static class ClipInit
        {
            private static void Prefix(AudioClipSimReadersController __instance)
            { if (Chuffs.TryGetValue(__instance, out var slot)) { Safe(slot.Remove); slot.Ready = false; } }
            private static void Postfix(AudioClipSimReadersController __instance, TrainCar car, SimulationFlow simFlow)
            {
                var slot = Chuffs.GetValue(__instance, _ => new ChuffBinding { Controller = __instance });
                slot.Car = car; slot.Flow = simFlow; slot.Ready = true;
                var state = ActiveState(car); if (state != null) Safe(state.RefreshNow);
            }
        }
        [HarmonyPatch(typeof(AudioClipSimReadersController), nameof(AudioClipSimReadersController.Deinit))]
        private static class ClipDeinit
        {
            private static void Prefix(AudioClipSimReadersController __instance)
            { if (Chuffs.TryGetValue(__instance, out var slot)) { Safe(slot.Remove); slot.Ready = false; slot.Car = null; } }
        }
        [HarmonyPatch(typeof(LayeredAudioSimReadersController), nameof(LayeredAudioSimReadersController.Init))]
        private static class LayerInit
        {
            private static void Prefix(LayeredAudioSimReadersController __instance)
            { if (Cocks.TryGetValue(__instance, out var slot)) { Safe(slot.Remove); slot.Ready = false; } }
            private static void Postfix(LayeredAudioSimReadersController __instance, TrainCar car, SimulationFlow simFlow)
            {
                var slot = Cocks.GetValue(__instance, _ => new CockBinding { Controller = __instance });
                slot.Car = car; slot.Flow = simFlow; slot.Ready = true;
                var state = ActiveState(car); if (state != null) Safe(state.RefreshNow);
            }
        }
        [HarmonyPatch(typeof(LayeredAudioSimReadersController), nameof(LayeredAudioSimReadersController.Deinit))]
        private static class LayerDeinit
        {
            private static void Prefix(LayeredAudioSimReadersController __instance)
            { if (Cocks.TryGetValue(__instance, out var slot)) { Safe(slot.Remove); slot.Ready = false; slot.Car = null; } }
        }
        [HarmonyPatch(typeof(ParticlesPortReadersController), nameof(ParticlesPortReadersController.Init))]
        private static class ParticleInit
        {
            private static void Prefix(ParticlesPortReadersController __instance)
            { ActiveState(TrainCar.Resolve(__instance.gameObject))?.RemoveParticles(__instance); }
            private static void Postfix(ParticlesPortReadersController __instance)
            { ActiveState(TrainCar.Resolve(__instance.gameObject))?.RefreshNow(); }
        }
        [HarmonyPatch(typeof(ParticlesPortReadersController), "OnDestroy")]
        private static class ParticleDestroy
        {
            private static void Prefix(ParticlesPortReadersController __instance)
            { var state = ActiveState(TrainCar.Resolve(__instance.gameObject)); if (state != null) Safe(() => state.RemoveParticles(__instance)); }
        }
    }

    internal sealed class ArticulatedEffectMarker : MonoBehaviour { }
}
#endif
