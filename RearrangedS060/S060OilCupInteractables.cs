#if B99_S060_FUNCTIONAL
using System;
using System.Collections.Generic;
using System.Linq;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using DV.Util;
using LocoSim.Definitions;
using LocoSim.Implementations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Owns only cloned external interactables and native controller enrollment.
    // Mount geometry and circuit selection belong to their existing owners.
    internal sealed class S060OilCupInteractables : IDisposable
    {
        private const string Prefix = "[S060 big-end cup ";
        private readonly TrainCar car;
        private readonly GameObject external;
        private readonly SimulationFlow flow;
        private readonly ManualOilingPoints points;
        private readonly ManualOilingPointsDefinition definition;
        private readonly List<GameObject> cups = new List<GameObject>();

        internal S060OilCupInteractables(TrainCar car, GameObject external)
        {
            this.car = car; this.external = external; flow = car.SimController.simFlow;
            points = S060Oiling.Points(flow); definition = S060Oiling.Definition(car);
            if (points?.oilingPoints.Length != S060Oiling.TotalPoints)
                throw new InvalidOperationException("Twelve native S060 oil circuits required.");
        }

        internal OilingPointPortFeederReader[] NativeReaders()
        {
            var native = external.GetComponentsInChildren<OilingPointPortFeederReader>(true).Where(r => !Ours(r.transform)).ToArray();
            if (native.Length != S060Oiling.NativePoints) throw new InvalidOperationException("Six original S060 oil cups required.");
            return Enumerable.Range(0, S060Oiling.NativePoints).Select(i => native.Single(r =>
                r.refillPortId == points.id + "." + definition.oilingPoints[i].refillExtIn.ID)).ToArray();
        }

        internal static PositionSyncConsumer Consumer(Component item)
        {
            for (var t = item.transform; t != null; t = t.parent)
            { var consumer = t.GetComponent<PositionSyncConsumer>(); if (consumer != null) return consumer; }
            return null;
        }

        internal PositionSyncConsumer Clone(OilingPointPortFeederReader source, int index, int nativeIndex)
        {
            var live = Consumer(source);
            string path = Relative(external.transform, live.transform);
            var prefab = car.isExploded && car.carLivery.explodedExternalInteractablesPrefab != null
                ? car.carLivery.explodedExternalInteractablesPrefab : car.carLivery.externalInteractablesPrefab;
            var template = prefab.transform.Find(path);
            if (template == null) throw new InvalidOperationException("Native oil cup prefab missing: " + path);
            var map = S060OilCupPorts.Map(points.id, definition.oilingPoints[nativeIndex], definition.oilingPoints[index]);
            var cup = Object.Instantiate(template.gameObject, live.transform.parent, false);
            cup.name = Prefix + index + "]"; cups.Add(cup); cup.AddComponent<S060OilCupLifetime>();
            foreach (var behaviour in cup.GetComponentsInChildren<MonoBehaviour>(true)) S060OilCupPorts.Rewrite(behaviour, map);
            var consumer = cup.GetComponent<PositionSyncConsumer>(); consumer.syncTag = "RS060_BIGEND_" + index;
            return consumer;
        }

        // Called after the geometry owner supplies the consumer's provider.
        internal void Register(PositionSyncConsumer consumer)
        {
            var cup = consumer.gameObject;
            Append(external.GetComponentInChildren<PositionSyncConsumerController>(true), consumer);
            foreach (var reader in cup.GetComponentsInChildren<OilingPointPortFeederReader>(true))
            {
                flow.TryGetPort(reader.refillPortId, out var refill);
                flow.TryGetPort(reader.refillingFlowNormalizedPortId, out var filling);
                reader.Deinit(); reader.Init(refill, filling);
                Append(external.GetComponentInChildren<OilingPointsPortController>(true), reader);
            }
            foreach (var indicator in cup.GetComponentsInChildren<IndicatorPortReader>(true))
            {
                flow.TryGetPort(indicator.portId, out var level);
                flow.TryGetPort(indicator.indicatorRangeScalerPortId, out var scaler, canBeNullOrEmpty: true);
                indicator.Deinit(); indicator.Init(level, null, scaler);
                Append(external.GetComponentInChildren<IndicatorPortReadersController>(true), indicator);
            }
            foreach (var audio in cup.GetComponentsInChildren<LayeredAudioPortReader>(true)) { audio.Deinit(); audio.Init(car, flow); }
        }

        // Native lid controls spawn after their parent; the coroutine owner
        // retains the established two-frame delay before calling this once.
        internal void BindLids()
        {
            if (car == null || external == null) return;
            var controller = external.GetComponentInChildren<InteractablePortFeedersController>(true);
            foreach (var cup in cups.Where(c => c != null))
                foreach (var feeder in cup.GetComponentsInChildren<InteractablePortFeeder>(true))
                    if (flow.TryGetPort(feeder.portId, out var port))
                    { feeder.Deinit(); feeder.Init(port, null); feeder.SetupControlChangedListeners(); Append(controller, feeder); }
        }

        public void Dispose()
        {
            if (external != null)
            {
                Remove(external.GetComponentInChildren<PositionSyncConsumerController>(true));
                Remove(external.GetComponentInChildren<OilingPointsPortController>(true));
                Remove(external.GetComponentInChildren<IndicatorPortReadersController>(true));
                Remove(external.GetComponentInChildren<InteractablePortFeedersController>(true));
            }
            foreach (var cup in cups.Where(c => c != null))
            { Deinit(cup); cup.SetActive(false); Object.Destroy(cup); }
            cups.Clear();
        }

        internal static void Deinit(GameObject cup)
        {
            foreach (var reader in cup.GetComponentsInChildren<OilingPointPortFeederReader>(true)) reader.Deinit();
            foreach (var feeder in cup.GetComponentsInChildren<InteractablePortFeeder>(true)) feeder.Deinit();
            foreach (var indicator in cup.GetComponentsInChildren<IndicatorPortReader>(true)) indicator.Deinit();
            foreach (var audio in cup.GetComponentsInChildren<LayeredAudioPortReader>(true)) audio.Deinit();
        }
        private static bool Ours(Transform transform)
        { for (var t = transform; t != null; t = t.parent) if (t.name.StartsWith(Prefix, StringComparison.Ordinal)) return true; return false; }
        private static string Relative(Transform root, Transform item)
        {
            var names = new List<string>();
            for (var t = item; t != null && t != root; t = t.parent) names.Add(t.name);
            names.Reverse(); return string.Join("/", names);
        }
        private static void Append<T>(ARefreshableChildrenController<T> controller, T item) where T : MonoBehaviour
        { if (controller != null && item != null) controller.entries = (controller.entries ?? Array.Empty<T>()).Where(e => e != null && e != item).Concat(new[] { item }).ToArray(); }
        private static void Remove<T>(ARefreshableChildrenController<T> controller) where T : MonoBehaviour
        { if (controller?.entries != null) controller.entries = controller.entries.Where(e => e != null && !Ours(e.transform)).ToArray(); }
    }

    internal sealed class S060OilCupLifetime : MonoBehaviour
    {
        private void OnDestroy() => S060OilCupInteractables.Deinit(gameObject);
    }
}
#endif
