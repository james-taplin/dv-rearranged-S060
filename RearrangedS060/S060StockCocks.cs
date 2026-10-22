#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DV.ModularAudioCar;
using DV.Simulation.Controllers;
using HarmonyLib;
using LocoSim.Implementations;

namespace RearrangedS060
{
    // Rebind only the native stock outlets to a zero flow while the two cloned
    // engine units own the cocks. Native controllers retain lifecycle ownership.
    internal sealed class S060StockCocks : IDisposable
    {
        private const string Muted = "rs060MutedCock.VALUE";
        private readonly TrainCar car;
        private readonly SimulationFlow flow;
        private static readonly ConditionalWeakTable<TrainCar, S060StockCocks> Owners = new ConditionalWeakTable<TrainCar, S060StockCocks>();
        private static readonly ConditionalWeakTable<CylinderCockLayeredPortReader, AudioHome> Audio = new ConditionalWeakTable<CylinderCockLayeredPortReader, AudioHome>();
        private readonly List<CylinderCockLayeredPortReader> audio = new List<CylinderCockLayeredPortReader>();
        private readonly List<(CylinderCockParticlePortReader reader, string id)> particles = new List<(CylinderCockParticlePortReader, string)>();
        private readonly List<(ParticlesPortReadersController.ParticlePortReader owner, ParticlesPortReadersController.ParticlePortReader.PortParticleUpdateDefinition updater, string id)> drips = new List<(ParticlesPortReadersController.ParticlePortReader, ParticlesPortReadersController.ParticlePortReader.PortParticleUpdateDefinition, string)>();
        internal S060StockCocks(TrainCar car) { this.car = car; flow = car.SimController.simFlow; }
        internal static void Register(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(CylinderCockLayeredPortReader), nameof(CylinderCockLayeredPortReader.Init)),
                prefix: new HarmonyMethod(typeof(S060StockCocks), nameof(AudioInit)));
            harmony.Patch(AccessTools.Method(typeof(CylinderCockLayeredPortReader), nameof(CylinderCockLayeredPortReader.Deinit)),
                postfix: new HarmonyMethod(typeof(S060StockCocks), nameof(AudioDeinit)));
        }
        private static void AudioInit(CylinderCockLayeredPortReader __instance, TrainCar car)
        {
            // Native audio is parented under TrainAudio before Initialize. The
            // two bogie copies have no TrainAudio ancestor and must stay live.
            if (car == null || !Owners.TryGetValue(car, out var owner) ||
                __instance.crankRotationPortId != "steamEngine.CRANK_ROTATION" ||
                __instance.GetComponentInParent<TrainAudio>() == null) return;
            if (Audio.TryGetValue(__instance, out _)) return;
            Audio.Add(__instance, new AudioHome(owner, __instance)); owner.audio.Add(__instance);
            __instance.cylinderCockFlowNormalizedPortId = Muted;
            // Native readers synthesize a moving cold-air hiss from the cab
            // control when steam flow is zero. Mute that input as well.
            __instance.cylinderCockControlPortId = Muted;
            foreach (var source in __instance.cylCockAudio) if (source != null) { source.Stop(); source.Reset(); }
        }
        private static void AudioDeinit(CylinderCockLayeredPortReader __instance)
        {
            if (!Audio.TryGetValue(__instance, out var home)) return;
            __instance.cylinderCockFlowNormalizedPortId = home.Flow;
            __instance.cylinderCockControlPortId = home.Control;
            home.Owner.audio.Remove(__instance); Audio.Remove(__instance);
        }
        private sealed class AudioHome
        {
            internal readonly S060StockCocks Owner;
            internal readonly string Flow, Control;
            internal AudioHome(S060StockCocks owner, CylinderCockLayeredPortReader reader)
            { Owner = owner; Flow = reader.cylinderCockFlowNormalizedPortId; Control = reader.cylinderCockControlPortId; }
        }
        internal void Mute()
        {
            Owners.Add(car, this);
            foreach (var reader in S060NativeAudio.Components<CylinderCockLayeredPortReader>(car).Where(r => r.crankRotationPortId == "steamEngine.CRANK_ROTATION" && r.GetComponentInParent<TrainAudio>() != null))
            {
                if (reader.crankRotationPort == null) continue;
                reader.Deinit(); reader.Init(car, flow);
            }
            foreach (var controller in car.GetComponentsInChildren<ParticlesPortReadersController>(true).Where(c => c.simFlow == flow))
            {
                foreach (var reader in controller.entries.OfType<CylinderCockParticlePortReader>().Where(r => r.crankRotationPortId == "steamEngine.CRANK_ROTATION"))
                {
                    particles.Add((reader, reader.cylinderCockFlowNormalizedPortId));
                    reader.Deinit(); ResetParticles(reader); reader.cylinderCockFlowNormalizedPortId = Muted; InitParticles(reader);
                    foreach (var setup in reader.cylinderSetups)
                        foreach (var p in setup.frontParticlesData.Concat(setup.rearParticlesData)) p.ps.Stop(true, UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                foreach (var reader in controller.particlePortReaders)
                    foreach (var updater in reader.particleUpdaters.Where(u => u.portId == "cylinderCock.EXT_IN"))
                    {
                        drips.Add((reader, updater, updater.portId)); updater.Deinit(flow); updater.portId = Muted; updater.Init(reader, flow);
                    }
            }
        }
        public void Dispose()
        {
            Owners.Remove(car);
            foreach (var reader in audio.ToArray()) if (reader != null)
            { reader.Deinit(); reader.Init(car, flow); }
            audio.Clear();
            foreach (var home in particles) if (home.reader != null)
            { home.reader.Deinit(); ResetParticles(home.reader); home.reader.cylinderCockFlowNormalizedPortId = home.id; InitParticles(home.reader); }
            particles.Clear();
            foreach (var home in drips) { home.updater.Deinit(flow); home.updater.portId = home.id; home.updater.Init(home.owner, flow); }
            drips.Clear();
        }
        private static void ResetParticles(CylinderCockParticlePortReader reader)
        {
            foreach (var setup in reader.cylinderSetups)
                foreach (var data in setup.frontParticlesData.Concat(setup.rearParticlesData))
                {
                    var main = data.ps.main; main.startSpeed = data.initialStartSpeed; main.startSize = data.initialStartSize;
                    var emission = data.ps.emission; emission.rateOverTime = data.initialRateOverTime;
                }
        }
        private void InitParticles(CylinderCockParticlePortReader reader)
        {
            // Native cocks disable their stopped ParticleSystem leaves. Init
            // enumerates active children only, so re-binding those leaves while
            // closed otherwise replaces the cached systems with empty arrays.
            foreach (var setup in reader.cylinderSetups)
                foreach (var root in new[] { setup.frontParticlesParent, setup.rearParticlesParent })
                    foreach (var ps in root.GetComponentsInChildren<UnityEngine.ParticleSystem>(true))
                        for (var node = ps.transform; node != null && node != reader.transform; node = node.parent)
                            node.gameObject.SetActive(true);
            reader.Init(flow);
        }
    }
}
#endif
