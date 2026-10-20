using System;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // The whole user interface of the P1 spike: two buttons in the mod manager
    // window. The comms radio is P4 work, and deliberately not touched here.
    internal static class S060SpikeWindow
    {
        private static string status = "Point at a stationary S060, or stand near one.";

        internal static void Draw()
        {
            var car = Target();

            GUILayout.Label("Rearranged S060 — P1 spike (0-4-0 only)");
            GUILayout.Label(car == null
                ? "No S060 found. Spawn one and stand near it."
                : "Target: " + S060FourCoupled.Describe(car));

            using (new GUILayout.HorizontalScope())
            {
                bool converted = car != null && S060FourCoupled.IsConverted(car);
                if (car != null && !converted && GUILayout.Button("CONVERT TO 0-4-0", GUILayout.Width(220)))
                    Run(() => S060FourCoupled.Convert(car), "Converted " + car.ID + " to 0-4-0.");
                if (car != null && converted && GUILayout.Button("RESTORE STOCK 0-6-0", GUILayout.Width(220)))
                    Run(() => S060FourCoupled.Restore(car), "Restored " + car.ID + " to 0-6-0.");
            }

            GUILayout.Label(status);
            GUILayout.Space(6);
            GUILayout.Label("Drops the middle driver and leaves drivers 1 and 3 coupled 3.10 m apart.\n" +
                            "Known cosmetic gaps: the coupling rod keeps its middle pin boss, and the\n" +
                            "brake shoes are one mesh so the middle shoe stays (its caliper is hidden).\n" +
                            "The bogie pivots are left where DV put them, so the car rides as stock.");
        }

        private static void Run(Action action, string success)
        {
            try
            {
                action();
                status = success;
            }
            catch (Exception ex)
            {
                status = ex.Message;
                Main.Logger.LogException("S060 P1 action failed", ex);
            }
        }

        // Same targeting rule the P0 probe used: what the player is looking at,
        // else the nearest S060.
        private static TrainCar Target()
        {
            Camera camera = null;
            try { camera = PlayerManager.ActiveCamera; } catch { }
            if (camera != null &&
                Physics.Raycast(camera.transform.position, camera.transform.forward, out var hit, 200f,
                                ~0, QueryTriggerInteraction.Ignore))
            {
                var looked = hit.transform != null ? hit.transform.GetComponentInParent<TrainCar>() : null;
                if (looked != null && S060Parts.IsS060(looked)) return looked;
            }

            Transform player = null;
            try { player = PlayerManager.PlayerTransform; } catch { }
            Vector3 from = player != null ? player.position
                : camera != null ? camera.transform.position : Vector3.zero;

            return UnityEngine.Object.FindObjectsOfType<TrainCar>()
                .Where(S060Parts.IsS060)
                .OrderBy(c => Vector3.Distance(from, c.transform.position))
                .FirstOrDefault();
        }
    }
}
