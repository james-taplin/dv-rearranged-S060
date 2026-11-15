#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using DV.ModularAudioCar;
using DV.Wheels;
using HarmonyLib;
using LocoSim.Implementations.Wheels;
using UnityEngine;

namespace RearrangedS060
{
    // S282 group-slip presentation, adapted to S060's replacement physical
    // axles. Native leaf emitters and layered audio retain their own assets;
    // damage audio, wheel-slide effects and the car's controls remain native.
    internal static class S060GroupSlipEffects
    {
        private sealed class SparkLease
        {
            internal WheelslipSparksController Controller;
            internal WheelslipSparksController.WheelSparksDefinition[] Original;
            internal readonly List<GameObject> Anchors = new List<GameObject>();
        }
        private sealed class AudioLease
        {
            internal WheelsAudioModule.WheelslipAudioDefinition Definition;
            internal List<PoweredWheel> Original;
        }
        private sealed class State
        {
            internal float NextTick;
            internal readonly Dictionary<WheelslipSparksController, SparkLease> Sparks = new Dictionary<WheelslipSparksController, SparkLease>();
            internal readonly List<WheelslipSparksController> Scan = new List<WheelslipSparksController>();
            internal readonly HashSet<WheelsAudioModule> Audio = new HashSet<WheelsAudioModule>();
            internal readonly List<AudioLease> AudioLeases = new List<AudioLease>();
        }
        private static readonly Dictionary<TrainCar, State> States = new Dictionary<TrainCar, State>();

        internal static void Register(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(WheelslipSparksController), "OnWheelslipStateChanged"),
                prefix: new HarmonyMethod(typeof(S060GroupSlipEffects), nameof(BeforeNativeSparkEvent)));
            harmony.Patch(AccessTools.Method(typeof(WheelsAudioModule), nameof(WheelsAudioModule.UpdateModule)),
                prefix: new HarmonyMethod(typeof(S060GroupSlipEffects), nameof(BeforeWheelAudio)),
                postfix: new HarmonyMethod(typeof(S060GroupSlipEffects), nameof(AfterWheelAudio)),
                finalizer: new HarmonyMethod(typeof(S060GroupSlipEffects), nameof(RestoreAudioDefinitions)));
        }
        private static State For(TrainCar car)
        {
            if (!States.TryGetValue(car, out var state)) States.Add(car, state = new State());
            return state;
        }
        private static bool BeforeNativeSparkEvent(WheelslipSparksController __instance) =>
            __instance.car == null || S060IndependentDrive.For(__instance.car)?.IsOwning != true;

        internal static void Tick(TrainCar car, S060IndependentDrive owner)
        {
            if (car == null || owner == null || !owner.IsOwning) return;
            var state = For(car);
            if (Time.time < state.NextTick) return;
            state.NextTick = Time.time + .1f;
            // Reuse the buffer and discover native streamed particle controllers
            // without a fresh component array each time the cab is occupied.
            car.GetComponentsInChildren(true, state.Scan);
            foreach (var controller in state.Scan)
            {
                if (controller == null || controller.car != car || controller.wheelSparks == null) continue;
                if (!state.Sparks.TryGetValue(controller, out var lease))
                {
                    lease = BindSparks(car, controller, owner);
                    if (lease == null) continue; // native leaf initialization is pending
                    state.Sparks.Add(controller, lease);
                }
                float ignition = 0;
                foreach (var definition in controller.wheelSparks)
                {
                    float slip = 0, rpm = 0;
                    bool emit = controller.isActiveAndEnabled && !car.derailed &&
                        definition.poweredWheel != null && definition.poweredWheel.IsPowered &&
                        owner.TryGetWheelState(definition.poweredWheel, out slip, out rpm) && slip > 0;
                    ignition = Mathf.Max(ignition, emit ? slip : 0);
                    UpdateParticle(definition.sparksLeftPS, controller, emit, rpm, slip);
                    UpdateParticle(definition.sparksRightPS, controller, emit, rpm, slip);
                }
                if (ignition > 0 && S060MultiplayerSync.IsSimulationAuthority) controller.Ignite(ignition);
            }
        }
        private static void UpdateParticle(ParticleSystem particle, WheelslipSparksController controller, bool emit, float rpm, float slip)
        {
            if (particle == null) return;
            if (!emit) { if (particle.isPlaying) particle.Stop(); return; }
            particle.transform.localRotation = Quaternion.Euler(rpm > 0 ? controller.directionForwardSparksRotation : controller.directionReverseSparksRotation);
            var main = particle.main; main.startSpeed = Mathf.Lerp(1.5f, 3f, slip);
            if (!particle.isPlaying) particle.Play();
        }
        private static bool SameBogie(TrainCar car, PoweredWheel a, PoweredWheel b)
        {
            if (a == null || b == null) return false;
            foreach (var bogie in car.Bogies)
                if (a.transform.IsChildOf(bogie.transform) && b.transform.IsChildOf(bogie.transform)) return true;
            return false;
        }
        private static SparkLease BindSparks(TrainCar car, WheelslipSparksController controller, S060IndependentDrive owner)
        {
            var original = controller.wheelSparks;
            var lease = new SparkLease { Controller = controller, Original = original };
            var definitions = new List<WheelslipSparksController.WheelSparksDefinition>();
            try
            {
                foreach (var wheel in car.SimController.poweredWheels.poweredWheels)
                {
                    if (!owner.TryGetWheelRadius(wheel, out float radius)) continue;
                    WheelslipSparksController.WheelSparksDefinition template = null;
                    foreach (var candidate in original)
                        if (candidate != null && SameBogie(car, candidate.poweredWheel, wheel)) { template = candidate; break; }
                    if (template == null) continue;
                    if (template.sparksLeftPS == null || template.sparksRightPS == null ||
                        template.sparksLeftAnchor == null || template.sparksRightAnchor == null) { DestroyAnchors(lease); return null; }
                    var definition = new WheelslipSparksController.WheelSparksDefinition { poweredWheel = wheel };
                    definition.sparksLeftAnchor = Anchor(wheel, template.poweredWheel, template.sparksLeftAnchor, radius, lease);
                    definition.sparksRightAnchor = Anchor(wheel, template.poweredWheel, template.sparksRightAnchor, radius, lease);
                    definition.sparksLeftPS = Particle(template.sparksLeftPS, definition.sparksLeftAnchor);
                    definition.sparksRightPS = Particle(template.sparksRightPS, definition.sparksRightAnchor);
                    definitions.Add(definition);
                }
                if (definitions.Count == 0) { DestroyAnchors(lease); return null; }
                controller.OnDisable();
                foreach (var definition in original)
                {
                    if (definition?.sparksLeftPS != null) definition.sparksLeftPS.Stop();
                    if (definition?.sparksRightPS != null) definition.sparksRightPS.Stop();
                }
                controller.wheelSparks = definitions.ToArray();
                return lease;
            }
            catch { controller.wheelSparks = original; DestroyAnchors(lease); throw; }
        }
        private static Transform Anchor(PoweredWheel wheel, PoweredWheel native, Transform original, float radius, SparkLease lease)
        {
            var anchor = new GameObject("[S060 group slip contact]"); lease.Anchors.Add(anchor);
            anchor.transform.SetParent(wheel.transform, false);
            var offset = native.transform.InverseTransformPoint(original.position);
            float radial = radius / S060Parts.DriverRadius;
            anchor.transform.localPosition = new Vector3(offset.x, offset.y * radial, offset.z * radial);
            return anchor.transform;
        }
        private static ParticleSystem Particle(ParticleSystem template, Transform anchor)
        {
            var copy = UnityEngine.Object.Instantiate(template.gameObject, anchor, false);
            copy.transform.localPosition = Vector3.zero;
            var particle = copy.GetComponent<ParticleSystem>();
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particle;
        }
        private static void BeforeWheelAudio(WheelsAudioModule __instance, out WheelsAudioModule.WheelslipAudioDefinition[] __state)
        {
            __state = null;
            var car = __instance.trainCar;
            if (car == null || S060IndependentDrive.For(car)?.IsOwning != true) return;
            var state = For(car);
            if (state.Audio.Add(__instance))
                foreach (var definition in __instance.wheelslipAudioDefs ?? Array.Empty<WheelsAudioModule.WheelslipAudioDefinition>())
                {
                    if (definition?.poweredWheels == null) continue;
                    var original = definition.poweredWheels;
                    var mapped = new List<PoweredWheel>();
                    foreach (var wheel in car.SimController.poweredWheels.poweredWheels)
                        foreach (var native in original)
                            if (SameBogie(car, native, wheel)) { mapped.Add(wheel); break; }
                    state.AudioLeases.Add(new AudioLease { Definition = definition, Original = original });
                    definition.poweredWheels = mapped;
                }
            __state = __instance.wheelslipAudioDefs;
            __instance.wheelslipAudioDefs = Array.Empty<WheelsAudioModule.WheelslipAudioDefinition>();
        }
        private static void RestoreAudioDefinitions(WheelsAudioModule __instance, WheelsAudioModule.WheelslipAudioDefinition[] __state)
        { if (__state != null) __instance.wheelslipAudioDefs = __state; }
        private static void AfterWheelAudio(WheelsAudioModule __instance, WheelsAudioModule.WheelslipAudioDefinition[] __state)
        {
            RestoreAudioDefinitions(__instance, __state);
            var car = __instance.trainCar;
            var owner = car != null ? S060IndependentDrive.For(car) : null;
            if (owner?.IsOwning != true || __instance.wheelslipAudioDefs == null) return;
            foreach (var definition in __instance.wheelslipAudioDefs)
            {
                if (definition?.poweredWheels == null) continue;
                float slip = 0;
                foreach (var wheel in definition.poweredWheels)
                    if (wheel != null && wheel.IsPowered && owner.TryGetWheelState(wheel, out float value, out _)) slip = Mathf.Max(slip, value);
                definition.wheelslipAudio?.Set(car.derailed ? 0 : slip);
                definition.derailedWheelslipAudio?.Set(car.derailed && car.groundFriction.IsGrounded ? slip : 0);
            }
        }
        private static void DestroyAnchors(SparkLease lease)
        {
            foreach (var anchor in lease.Anchors)
                if (anchor != null) { anchor.SetActive(false); if (Application.isPlaying) UnityEngine.Object.Destroy(anchor); else UnityEngine.Object.DestroyImmediate(anchor); }
            lease.Anchors.Clear();
        }
        internal static void Release(TrainCar car)
        {
            if (ReferenceEquals(car, null) || !States.TryGetValue(car, out var state)) return;
            States.Remove(car);
            var errors = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
            foreach (var lease in state.AudioLeases)
            {
                Attempt(() => lease.Definition.wheelslipAudio?.Set(0));
                Attempt(() => lease.Definition.derailedWheelslipAudio?.Set(0));
                lease.Definition.poweredWheels = lease.Original;
            }
            foreach (var lease in state.Sparks.Values)
            {
                if (lease.Controller != null)
                {
                    Attempt(lease.Controller.OnDisable);
                    lease.Controller.wheelSparks = lease.Original;
                }
                Attempt(() => DestroyAnchors(lease));
                var controller = lease.Controller;
                if (car != null && controller != null && controller.isActiveAndEnabled &&
                    controller.wheelslipController?.IsWheelslipping == true && controller.updateSparksCoro == null)
                    Attempt(() => controller.OnWheelslipStateChanged(true));
            }
            if (errors.Count != 0) throw new AggregateException("S060 group slip effect cleanup failed", errors);
        }
    }
}
#endif
