using UnityEngine;

namespace RearrangedS060
{
    // IMGUI calls Draw more than once per frame. Never search every Unity
    // object from it: DV already owns the authoritative list of spawned cars.
    internal static class S060Targeting
    {
        private static TrainCar cached;
        private static float nextRefresh;

        internal static TrainCar Target()
        {
            // Keep one target throughout the layout/input/repaint sequence.
            var guiEvent = Event.current;
            if (guiEvent != null && guiEvent.type != EventType.Layout) return cached;
            float now = Time.realtimeSinceStartup;
            if (now < nextRefresh) return cached;
            nextRefresh = now + .25f;
            cached = FindTarget();
            return cached;
        }

        private static TrainCar FindTarget()
        {
            Camera camera = null;
            Transform player = null;
            try { camera = PlayerManager.ActiveCamera; } catch { /* No player in the main menu. */ }
            try { player = PlayerManager.PlayerTransform; } catch { /* No player in the main menu. */ }
            if (camera == null && player == null) return null;

            if (camera != null && Physics.Raycast(camera.transform.position, camera.transform.forward,
                out var hit, 100f, ~0, QueryTriggerInteraction.Ignore))
            {
                var pointed = hit.transform.GetComponentInParent<TrainCar>();
                if (S060Parts.IsS060(pointed) && pointed.logicCar != null) return pointed;
            }

            var spawner = CarSpawner.Instance;
            if (spawner == null || spawner.AllCars == null) return null;
            var from = player != null ? player.position : camera.transform.position;
            TrainCar nearest = null;
            float bestDistanceSquared = 30f * 30f;
            foreach (var candidate in spawner.AllCars)
            {
                if (!S060Parts.IsS060(candidate) || candidate.logicCar == null || !candidate.gameObject.activeInHierarchy) continue;
                float distanceSquared = (candidate.transform.position - from).sqrMagnitude;
                if (distanceSquared > bestDistanceSquared) continue;
                bestDistanceSquared = distanceSquared;
                nearest = candidate;
            }
            return nearest;
        }
    }
}
