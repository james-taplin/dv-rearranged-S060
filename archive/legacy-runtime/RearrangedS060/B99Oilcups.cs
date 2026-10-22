#if B99_ALL_STANDARD
using System;
using System.Linq;
using DV.Util;
using DV.Simulation.Controllers;
using UnityEngine;

namespace RearrangedS060
{
    // Keep B99's six native circuits, oil quantities and refill controllers.
    // Their providers follow the original crosshead/main-rod transforms, including
    // their arrangement-specific translation and scale. No new oil simulations.
    internal static class B99Oilcups
    {
        internal static void Bind(TrainCar car, GameObject external)
        {
            if (external == null) return;
            var providers = car.GetComponent<PositionSyncProviderController>()?.entries;
            var consumers = external.GetComponent<PositionSyncConsumerController>()?.entries;
            var oiling = external.GetComponent<OilingPointsPortController>()?.entries;
            if (providers == null || consumers == null || oiling == null || oiling.Length != 6)
                throw new InvalidOperationException("S282 native six-cup oiling controllers are not initialized.");
            foreach (var cup in oiling)
            {
                if (!car.SimController.simFlow.TryGetPort(cup.refillPortId, out var refill) ||
                    !car.SimController.simFlow.TryGetPort(cup.refillingFlowNormalizedPortId, out var flow))
                    throw new InvalidOperationException("S282 oilcup simulation port missing: " + cup.refillPortId);
                var consumer = cup.GetComponentInParent<PositionSyncConsumer>();
                if (consumer == null || !consumers.Contains(consumer))
                    throw new InvalidOperationException("S282 oilcup position consumer was not found.");
                var matching = providers.Where(p => p != null && p.syncTag == consumer.syncTag).ToArray();
                if (matching.Length != 1)
                    throw new InvalidOperationException("S282 oilcup needs one native position provider: " + consumer.syncTag);
                consumer.SetProviderTransform(matching[0]);
                consumer.Sync();
            }
        }
    }
}
#endif
