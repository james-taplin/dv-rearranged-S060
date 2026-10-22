#if B99_MEYER
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DV.Simulation.Cars;
using DV.ModularAudioCar;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using LocoSim.Implementations;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using HarmonyLib;
using LocoSim.Definitions;
using UnityEngine;

namespace RearrangedS060
{
    internal sealed class MeyerRuntime : MonoBehaviour
    {
        internal const string LiveryId = "RS060_Meyer_0440", HpId = "steamEngine", LpId = "meyerLpEngine";
        private SimConnectionDefinition original, owned;
        private GameObject definitions;
        private CylinderCockLayeredPortReader[] cockAudio = Array.Empty<CylinderCockLayeredPortReader>();
        private SimulationFlow audioFlow;
        private ParticlesPortReadersController[] cockParticles = Array.Empty<ParticlesPortReadersController>();
        private bool presentationInitialized;
        private Coroutine pendingPresentation;
        private S060SteamPipes steamPipes;
#if B99_DOCKSIDE
        private AnimatorPortReader[] motionReaders = Array.Empty<AnimatorPortReader>();
        private SimulationFlow phasedFlow;
#endif
        internal static bool IsMeyer(TrainCar car) => car != null && car.carLivery != null && car.carLivery.id == LiveryId;
        private static bool HasSharedCab(TrainCar car)
        {
#if B99_DOCKSIDE
            if (car != null && car.carLivery != null && car.carLivery.id == "RS060_Franklin_Dockside_0440") return true;
#endif
            return IsMeyer(car);
        }
        internal static void Register(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(Initializing)).Patch();
            harmony.CreateClassProcessor(typeof(ParticleDestroy)).Patch();
            harmony.CreateClassProcessor(typeof(CabBeforeAwake)).Patch();
        }

        private void Prepare(SimController controller, TrainCar car)
        {
            StopPresentation();
            if (owned != null && controller.connectionsDefinition == owned) controller.connectionsDefinition = original;
            if (definitions != null) { definitions.SetActive(false); Destroy(definitions); }
            original = controller.connectionsDefinition;
            if (original == null) throw new InvalidOperationException("Meyer CCL steam graph is missing.");
            var hp = original.executionOrder.OfType<ReciprocatingSteamEngineDefinition>().Single(d => d.ID == HpId);
            var lp = original.executionOrder.OfType<ReciprocatingSteamEngineDefinition>().Single(d => d.ID == LpId);
            Require(hp.numCylinders == 2 && lp.numCylinders == 2 && hp.pistonStroke == lp.pistonStroke, "Meyer requires two matching-stroke cylinder pairs.");
            Require(original.portReferenceConnections.Single(c => c.portReferenceId == HpId + ".CYLINDER_COCK_CONTROL").portId == "cylinderCock.EXT_IN" &&
                original.portReferenceConnections.Single(c => c.portReferenceId == LpId + ".CYLINDER_COCK_CONTROL").portId == "cylinderCock.EXT_IN", "Both engines must use the cab cock lever.");
            Require(original.portReferenceConnections.Single(c => c.portReferenceId == "steamConsumptionCalculator.ENGINE").portId == HpId + ".INTAKE_FLOW", "Only HP may draw engine steam from the boiler.");
            Require(original.portReferenceConnections.Single(c => c.portReferenceId == "exhaust.EXHAUST_FLOW").portId == LpId + ".EXHAUST_FLOW", "Only LP exhaust may reach the blast pipe.");
            Require(original.portReferenceConnections.Single(c => c.portReferenceId == LpId + ".INTAKE_PRESSURE").portId == HpId + ".EXHAUST_PRESSURE" &&
                original.portReferenceConnections.Single(c => c.portReferenceId == LpId + ".INTAKE_TEMPERATURE").portId == HpId + ".EXHAUST_TEMPERATURE", "LP must read receiver supply, not boiler supply.");
            Require(Array.IndexOf(original.executionOrder, hp) < Array.IndexOf(original.executionOrder, lp), "HP must advance the compound before LP reads it.");
            var cycle = new MeyerCycleOwner
            {
                Cycle = new MeyerCompoundCycle(hp.cylinderBore, lp.cylinderBore, hp.pistonStroke,
                    hp.steamChestVolume / 1000, lp.steamChestVolume / 1000, hp.throttleMaxFlow)
            };
            definitions = new GameObject("[Meyer compound definitions]"); definitions.transform.SetParent(controller.transform, false);
            owned = definitions.AddComponent<SimConnectionDefinition>();
            var hpOwned = Clone(hp, cycle, true); var lpOwned = Clone(lp, cycle, false);
            owned.executionOrder = original.executionOrder.Select(d => d == hp ? hpOwned : d == lp ? lpOwned : d).ToArray();
            owned.connections = original.connections; owned.portReferenceConnections = original.portReferenceConnections;
            controller.connectionsDefinition = owned;

            Main.Logger.Log("Meyer compound prepared: shared S060 cab cocks, HP -> receiver -> LP, HP-only boiler draw.");
        }

        private static void AssignCab(TrainCar car)
        {
#if B99_DOCKSIDE
            DocksideBody.Prepare(car);
#endif
            // Awake calls SetupInterior before SimController.Initialize. The
            // standard livery fields must therefore be assigned at Awake entry.
            // Never edit the S060 donor or any unrelated custom livery.
            var donor = TrainCarType.LocoS060.ToV2();
            Require(donor != null && donor.interiorPrefab != null, "Native S060 cab is unavailable.");
            S060CabCompatibility.Prepare(car, donor.prefab);
            car.carLivery.interiorPrefab = donor.interiorPrefab;
            car.carLivery.explodedInteriorPrefab = donor.explodedInteriorPrefab;
            car.carLivery.externalInteractablesPrefab = donor.externalInteractablesPrefab;
            car.carLivery.explodedExternalInteractablesPrefab = donor.explodedExternalInteractablesPrefab;
            if (car.transform.Find("[interior LOD]") == null)
            {
                var lod = donor.prefab.transform.Find("[interior LOD]");
                Require(lod != null, "Native S060 interior LOD is unavailable.");
                var copy = Instantiate(lod.gameObject, car.transform); copy.name = "[interior LOD]";
            }
        }

        private void BindPresentation(TrainCar car, SimulationFlow flow)
        {
            StopPresentation();
            var pipeFrames = car.GetComponentsInChildren<Transform>(true).Where(t => t.name == "bogie_car").OrderBy(t => car.transform.InverseTransformPoint(t.position).z).ToArray();
            if (pipeFrames.Length == 2) steamPipes = S060SteamPipes.Bind(car.transform, pipeFrames[1], pipeFrames[0], car.transform);
#if B99_DOCKSIDE
            if (!IsMeyer(car))
            {
                // SteamExhaust caches capacity during initialization, before
                // native tick-only sums have run. Seed that constant sum once.
                flow.OrderedSimComps.OfType<MultiplePortSum>().Single(c => c.id == "docksideMaxFlow").Tick(0);
                flow.OrderedSimComps.OfType<SteamExhaust>().Single().InitializationAfterConnecting();
            }
                if (!ReferenceEquals(phasedFlow, flow)) { DocksidePhasing.Seed(flow.OrderedSimComps); phasedFlow = flow; }
                motionReaders = car.GetComponentsInChildren<AnimatorPortReader>(true)
                    .Where(r => r.portId == HpId + ".CRANK_ROTATION" || r.portId == LpId + ".CRANK_ROTATION").ToArray();
                Require(motionReaders.Length == 2, "Meyer/Dockside requires two native crank animation readers.");
                foreach (var reader in motionReaders)
                {
                    Require(flow.TryGetPort(reader.portId, out var port), "Dockside crank port missing: " + reader.portId);
                    reader.Init(port);
                }
#endif
            cockAudio = car.GetComponentsInChildren<CylinderCockLayeredPortReader>(true);
            Require(cockAudio.Length == 2, "Meyer requires one native cock sound reader per engine.");
            foreach (var reader in cockAudio)
            {
                foreach (string id in new[] { reader.crankRotationPortId, reader.cylindersInletValveOpenPortId,
                    reader.cylinderCockFlowNormalizedPortId, reader.cylinderCockControlPortId })
                    Require(flow.TryGetPort(id, out _), "Meyer cock audio port missing: " + id);
                Require(reader.cylCockAudio.Length == 2 && reader.cylCockAudio.All(a => a != null), "Meyer native cock audio aliases did not resolve.");
            }
            audioFlow = flow;
            foreach (var reader in cockAudio) reader.Init(car, flow);
            cockParticles = car.GetComponentsInChildren<ParticlesPortReadersController>(true)
                .Where(p => p.name == "[Meyer engine effects]").ToArray();
            Require(cockParticles.Length == 2, "Meyer requires one cock particle controller per engine.");
            foreach (var controller in cockParticles) { PrepareCockParticles(controller); controller.Init(flow); ValidateCockParticles(controller); }
            presentationInitialized = true;
        }

        internal static void PrepareCockParticles(ParticlesPortReadersController controller)
        {
            // The owned cloned frame is authoritative. Rebuild the native
            // controller's child list before binding instead of retaining any
            // imported donor references across Instantiate/pooling.
            controller.entries = controller.GetComponentsInChildren<AParticlePortReader>(true);
            Require(controller.entries.OfType<CylinderCockParticlePortReader>().Count() == 1,
                "Owned engine effects require one cylinder-cock particle reader.");
            // Native readers enumerate active children. Copied particle aliases
            // may be inactive, and Initialize runs before bogie activation.
            // Only these owned prototype effect roots are changed.
            var roots = controller.entries.OfType<CylinderCockParticlePortReader>().SelectMany(r => r.cylinderSetups)
                .SelectMany(s => new[] { s.frontParticlesParent, s.rearParticlesParent })
                .Concat(controller.particlePortReaders.Select(r => r.particlesParent)).Distinct();
            foreach (var root in roots)
            {
                Require(root != null, "Cylinder-cock particle outlet is missing.");
                var systems = root.GetComponentsInChildren<ParticleSystem>(true);
                Require(systems.Length > 0, "Native particle alias unresolved: " + root.name);
                foreach (var ps in systems)
                {
                    for (var node = ps.transform; node != null && node != controller.transform; node = node.parent)
                        node.gameObject.SetActive(true);
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = ps.main; main.playOnAwake = false;
                }
            }
        }

        internal static void ValidateCockParticles(ParticlesPortReadersController controller)
        {
            foreach (var reader in controller.entries.OfType<CylinderCockParticlePortReader>())
                foreach (var setup in reader.cylinderSetups)
                    Require(setup.frontParticlesData != null && setup.frontParticlesData.Length > 0 &&
                        setup.rearParticlesData != null && setup.rearParticlesData.Length > 0,
                        "Owned cylinder-cock reader did not capture its active particle aliases.");
        }

        private void SchedulePresentation(TrainCar car, SimulationFlow flow)
        {
            StopPresentation();
            presentationInitialized = true; // retain intent if disabled before binding
            pendingPresentation = StartCoroutine(BindAfterActivation(car, flow));
        }
        private IEnumerator BindAfterActivation(TrainCar car, SimulationFlow flow)
        {
            // TrainCar.Awake finishes activating both native bogies after the
            // SimController.Initialize postfix. Bind active particle hierarchies.
            yield return null;
            var controllers = car == null ? Array.Empty<ParticlesPortReadersController>() : car.GetComponentsInChildren<ParticlesPortReadersController>(true)
                .Where(c => c.name == "[Meyer engine effects]").ToArray();
            while (car != null && isActiveAndEnabled && car.SimController?.simFlow == flow &&
                (!car.AreBogiesFullyInitialized() || controllers.Any(c => c != null && !c.gameObject.activeInHierarchy)))
                yield return null;
            pendingPresentation = null;
            if (car != null && isActiveAndEnabled && car.SimController?.simFlow == flow)
            {
                try { BindPresentation(car, flow); }
                catch (Exception ex) { StopPresentation(); Main.Logger.LogException("Meyer/Dockside presentation initialization failed", ex); }
            }
        }

        private void StopPresentation()
        {
            steamPipes?.Dispose(); steamPipes = null;
            if (pendingPresentation != null) { StopCoroutine(pendingPresentation); pendingPresentation = null; }
#if B99_DOCKSIDE
            foreach (var reader in motionReaders) if (reader != null) reader.Deinit();
            motionReaders = Array.Empty<AnimatorPortReader>();
#endif
            if (audioFlow != null)
                foreach (var reader in cockAudio)
                    if (reader != null)
                    {
                        reader.Deinit();
                        foreach (var audio in reader.cylCockAudio) if (audio != null) audio.Set(0);
                    }
            audioFlow = null; cockAudio = Array.Empty<CylinderCockLayeredPortReader>();
            foreach (var controller in cockParticles)
                if (controller != null && controller.simFlow != null)
                {
                    // Use the native complete teardown (readers, drips, pause
                    // events), then mark it unbound before a second Init.
                    controller.OnDestroy(); controller.simFlow = null;
                    foreach (var reader in controller.entries.OfType<CylinderCockParticlePortReader>())
                        foreach (var setup in reader.cylinderSetups)
                            foreach (var data in setup.frontParticlesData.Concat(setup.rearParticlesData))
                                if (data.ps != null)
                                {
                                    var main = data.ps.main; main.startSpeed = data.initialStartSpeed; main.startSize = data.initialStartSize;
                                    var emission = data.ps.emission; emission.rateOverTime = data.initialRateOverTime;
                                    data.ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                                }
                }
            cockParticles = Array.Empty<ParticlesPortReadersController>();
        }
        private void OnDisable() => StopPresentation();
        private void OnDestroy() => StopPresentation();
        private void OnEnable()
        {
            var car = GetComponent<TrainCar>();
            if (presentationInitialized && HasSharedCab(car) && car.SimController?.simFlow != null)
                SchedulePresentation(car, car.SimController.simFlow);
        }

        private MeyerStageDefinition Clone(ReciprocatingSteamEngineDefinition source, MeyerCycleOwner cycle, bool hp)
        {
            var target = definitions.AddComponent<MeyerStageDefinition>();
            foreach (var f in typeof(ReciprocatingSteamEngineDefinition).GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (!f.IsInitOnly && (f.FieldType == typeof(float) || f.FieldType == typeof(int))) f.SetValue(target, f.GetValue(source));
            target.ID = source.ID; target.Owner = cycle; target.HighPressure = hp;
            return target;
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

        [HarmonyPatch(typeof(SimController), nameof(SimController.Initialize))]
        private static class Initializing
        {
            private static void Prefix(SimController __instance, TrainCar trainCar)
            {
                if (!HasSharedCab(trainCar)) return;
                var owner = trainCar.GetComponent<MeyerRuntime>() ?? trainCar.gameObject.AddComponent<MeyerRuntime>();
                // Abort a malformed Meyer spawn rather than silently driving it
                // with a simple fallback and calling it a compound.
                if (IsMeyer(trainCar)) owner.Prepare(__instance, trainCar);
            }
            private static void Postfix(SimController __instance, TrainCar trainCar)
            {
                if (HasSharedCab(trainCar)) trainCar.GetComponent<MeyerRuntime>().SchedulePresentation(trainCar, __instance.simFlow);
            }
        }

        [HarmonyPatch(typeof(ParticlesPortReadersController), "OnDestroy")]
        private static class ParticleDestroy
        {
            // An owned controller already torn down on pool return must not
            // deinitialize the same native readers again on final destruction.
            private static bool Prefix(ParticlesPortReadersController __instance) =>
                __instance.name != "[Meyer engine effects]" || __instance.simFlow != null;
        }

        [HarmonyPatch(typeof(TrainCar), "Awake")]
        private static class CabBeforeAwake
        {
            private static void Prefix(TrainCar __instance) { if (HasSharedCab(__instance)) AssignCab(__instance); }
        }
    }
}
#endif
