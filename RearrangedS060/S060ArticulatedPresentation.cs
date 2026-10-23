#if B99_S060_ARTICULATED
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DV.ModularAudioCar;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using LocoSim.Implementations;
using UnityEngine;

namespace RearrangedS060
{
    // Every subscription belongs to one cloned bogie frame and is removed before
    // that frame is destroyed. The original car audio/particle controllers stay.
    internal sealed class S060ArticulatedPresentation : IDisposable
    {
        private readonly TrainCar car;
        private readonly GameObject root;
        private readonly string engine;
        private readonly bool animated;
        private readonly List<CylinderCockLayeredPortReader> audio = new List<CylinderCockLayeredPortReader>();
        private readonly List<ParticlesPortReadersController> particles = new List<ParticlesPortReadersController>();
        private readonly List<AnimatorPortReader> motion = new List<AnimatorPortReader>();
        private readonly List<S060CockTiming> cockTiming = new List<S060CockTiming>();
        private S060CrankWheels wheels;
        private Port cockControl;
        private Coroutine pendingCockCheck;
        internal S060ArticulatedPresentation(TrainCar car, GameObject root, string engine, bool animated)
        { this.car = car; this.root = root; this.engine = engine; this.animated = animated; }
        private string Remap(string value) => value?.Replace(MeyerRuntime.LpId + ".", engine + ".").Replace(MeyerRuntime.HpId + ".", engine + ".");
        internal void Bind()
        {
            var flow = car.SimController.simFlow;
            foreach (var reader in root.GetComponentsInChildren<AnimatorPortReader>(true))
            {
                reader.portId = Remap(reader.portId);
                if (!flow.TryGetPort(reader.portId, out var port)) throw new InvalidOperationException("Articulated animation port missing: " + reader.portId);
                motion.Add(reader); reader.Init(port);
            }
            if (animated && motion.Count != 1) throw new InvalidOperationException("Dockside bogie requires one native crank animation reader.");
            if (!animated && motion.Count == 0)
            {
                wheels = root.AddComponent<S060CrankWheels>();
                flow.TryGetPort(engine + ".CRANK_ROTATION", out var crank);
                wheels.Bind(crank, root.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("driver R") || t.name.Contains("driver L")).ToArray());
            }
            foreach (var reader in root.GetComponentsInChildren<CylinderCockLayeredPortReader>(true))
            {
                reader.crankRotationPortId = Remap(reader.crankRotationPortId);
                reader.cylindersInletValveOpenPortId = Remap(reader.cylindersInletValveOpenPortId);
                reader.cylinderCockFlowNormalizedPortId = Remap(reader.cylinderCockFlowNormalizedPortId);
                foreach (string id in new[] { reader.crankRotationPortId, reader.cylindersInletValveOpenPortId, reader.cylinderCockFlowNormalizedPortId, reader.cylinderCockControlPortId })
                    if (!flow.TryGetPort(id, out _)) throw new InvalidOperationException("Articulated cock audio port missing: " + id);
                if (reader.cylCockAudio.Length != 2 || reader.cylCockAudio.Any(a => a == null)) throw new InvalidOperationException("Articulated cock audio aliases are unresolved.");
                audio.Add(reader); reader.Init(car, flow);
            }
            foreach (var controller in root.GetComponentsInChildren<ParticlesPortReadersController>(true))
            {
                controller.selfInitialization = false;
                MeyerRuntime.PrepareCockParticles(controller);
                foreach (var reader in controller.entries.OfType<CylinderCockParticlePortReader>())
                {
                    reader.crankRotationPortId = Remap(reader.crankRotationPortId);
                    reader.cylindersInletValveOpenPortId = Remap(reader.cylindersInletValveOpenPortId);
                    reader.cylinderCockFlowNormalizedPortId = Remap(reader.cylinderCockFlowNormalizedPortId);
                }
                foreach (var reader in controller.particlePortReaders)
                    foreach (var updater in reader.particleUpdaters)
                    {
                        updater.portId = Remap(updater.portId);
                        if (!flow.TryGetPort(updater.portId, out _)) throw new InvalidOperationException("Articulated particle port missing: " + updater.portId);
                    }
                particles.Add(controller); controller.Init(flow);
                MeyerRuntime.ValidateCockParticles(controller);
                foreach (var reader in controller.entries.OfType<CylinderCockParticlePortReader>())
                    cockTiming.Add(S060CockTiming.Bind(reader));
            }
            if (audio.Count != 1 || particles.Count != 1) throw new InvalidOperationException("Articulated bogie requires one native cock audio and particle controller.");
            flow.TryGetPort(audio[0].cylinderCockControlPortId, out cockControl);
            cockControl.ValueUpdatedInternally += CockControlChanged;
        }
        private void CockControlChanged(float control)
        {
            if (pendingCockCheck != null) { car.StopCoroutine(pendingCockCheck); pendingCockCheck = null; }
            if (control > 0 && car.isActiveAndEnabled) pendingCockCheck = car.StartCoroutine(CheckCockState());
        }
        private IEnumerator CheckCockState()
        {
            // A bounded snapshot after the cab input has reached the live
            // engine distinguishes absent steam from absent VFX next test.
            yield return null; yield return null;
            pendingCockCheck = null;
            var flow = car.SimController?.simFlow;
            if (flow == null || root == null || cockControl == null || cockControl.Value <= 0) yield break;
            flow.TryGetPort(engine + ".CYLINDER_COCK_FLOW_NORMALIZED", out var vent);
            flow.TryGetPort(engine + ".STEAM_CHEST_PRESSURE", out var pressure);
            flow.TryGetPort(engine + ".CYLINDERS_INLET_VALVE_OPEN", out var inlet);
            flow.TryGetPort(engine + ".WATER_IN_CYLINDERS_NORMALIZED", out var water);
            flow.TryGetPort(engine + ".CYLINDER_TEMPERATURE", out var temperature);
            var jets = particles.SelectMany(c => c.entries.OfType<CylinderCockParticlePortReader>()).ToArray();
            var data = jets
                .SelectMany(r => r.cylinderSetups).SelectMany(s => s.frontParticlesData.Concat(s.rearParticlesData)).ToArray();
            var drips = particles.SelectMany(c => c.particlePortReaders).Where(r => r.particlesParent.name.StartsWith("Water drip ", StringComparison.Ordinal)).ToArray();
            var dripData = drips.Where(r => r.particleSystemsData != null).SelectMany(r => r.particleSystemsData).ToArray();
            Main.Logger.Log("S060 cock check " + car.ID + " " + engine + ": cab=" + cockControl.Value.ToString("0.###") +
                ", flow=" + (vent?.Value ?? -1).ToString("0.###") + ", chest=" + (pressure?.Value ?? -1).ToString("0.###") +
                ", inlet=" + (inlet?.Value ?? -1).ToString("0") + ", cached=" + data.Length +
                ", emitting=" + data.Count(p => p.ps != null && p.ps.isEmitting) +
                ", flowBound=" + jets.All(r => r.cylinderCockFlowNormalizedPort == vent && r.cylindersInletValveOpenPort == inlet) +
                ", controllersActive=" + particles.All(c => c.isActiveAndEnabled) +
                ", water=" + (water?.Value ?? -1).ToString("0.####") + ", cylinderC=" + (temperature?.Value ?? -1).ToString("0.##") +
                ", dripCached=" + dripData.Length + ", dripEmitting=" + dripData.Count(p => p.ps != null && p.ps.isEmitting) +
                ", dripGates=" + string.Join(";", drips.Select(r => string.Join("/", r.particleUpdaters.Select(u => u.activationConditionFulfilled ? "1" : "0")))));
        }
        public void Dispose()
        {
            if (pendingCockCheck != null && car != null) car.StopCoroutine(pendingCockCheck);
            pendingCockCheck = null;
            if (cockControl != null) cockControl.ValueUpdatedInternally -= CockControlChanged;
            cockControl = null;
            foreach (var reader in motion) if (reader != null) reader.Deinit(); motion.Clear();
            if (wheels != null) wheels.enabled = false;
            foreach (var reader in audio)
                if (reader != null) { reader.Deinit(); foreach (var source in reader.cylCockAudio) if (source != null) source.Set(0); }
            audio.Clear();
            foreach (var timing in cockTiming) timing.Dispose();
            cockTiming.Clear();
            foreach (var controller in particles)
                if (controller != null && controller.simFlow != null) { controller.OnDestroy(); controller.simFlow = null; }
            particles.Clear();
            if (root != null) foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    internal sealed class S060CrankWheels : MonoBehaviour
    {
        private Port crank;
        private Transform[] wheels;
        private Quaternion[] rotations;
        internal void Bind(Port crank, Transform[] wheels)
        {
            this.crank = crank ?? throw new ArgumentNullException(nameof(crank)); this.wheels = wheels;
            rotations = wheels.Select(t => t.localRotation).ToArray();
            LateUpdate();
        }
        private void LateUpdate()
        {
            if (crank == null || wheels == null) return;
            var rotation = Quaternion.AngleAxis(360 * crank.Value, Vector3.right);
            for (int i = 0; i < wheels.Length; i++) if (wheels[i] != null) wheels[i].localRotation = rotation * rotations[i];
        }
    }
}
#endif
