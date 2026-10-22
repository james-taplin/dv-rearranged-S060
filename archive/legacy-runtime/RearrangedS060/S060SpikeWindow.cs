using System;
using UnityEngine;

namespace RearrangedS060
{
    // The whole user interface of the spike builds: a few buttons in the mod
    // manager window. The comms radio is P4 work, and deliberately not touched.
    internal static class S060SpikeWindow
    {
        private static string status = "Point at a stationary S060, or stand near one.";
        private static float driverScale = S060DriverScale.Stock;

        internal static void Draw()
        {
            var car = S060Targeting.Target();
            var current = car == null ? null : S060Conversion.Current(car);

            GUILayout.Label("Rearranged S060 — spike builds (0-4-0, 0-8-0, driver resize)");
            GUILayout.Label(car == null
                ? "No S060 found. Spawn one and stand near it."
                : "Target: " + S060Conversion.Describe(car));

            GUILayout.Space(4);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Driver size " + (2f * S060Parts.DriverRadius * driverScale).ToString("0.00") +
                    " m  (scale " + driverScale.ToString("0.00") + ")", GUILayout.Width(260));
                driverScale = GUILayout.HorizontalSlider(driverScale,
                    S060DriverScale.Smallest, S060DriverScale.Largest, GUILayout.Width(220));
                if (GUILayout.Button("STOCK SIZE", GUILayout.Width(110)))
                    driverScale = S060DriverScale.Stock;
            }
            GUILayout.Label("Scale is read when you convert. To try another size, restore and convert again.");
            GUILayout.Label("Experimental layout scale: gauge stays stock; axle spacing and cylinder position also change.");
            GUILayout.Space(4);

            using (new GUILayout.HorizontalScope())
            {
                if (car != null && current == null)
                {
                    if (GUILayout.Button("CONVERT TO 0-4-0", GUILayout.Width(200)))
                        Run(() => S060Conversion.Convert<S060FourCoupled>(car, driverScale), "Converted " + car.ID + " to 0-4-0.");
                    if (GUILayout.Button("CONVERT TO 0-8-0", GUILayout.Width(200)))
                        Run(() => S060Conversion.Convert<S060EightCoupled>(car, driverScale), "Converted " + car.ID + " to 0-8-0.");
                    if (GUILayout.Button("RESIZE DRIVERS ONLY", GUILayout.Width(200)))
                        Run(() => S060Conversion.Convert<S060Rescaled>(car, driverScale), "Resized " + car.ID + "'s drivers.");
                }
                if (car != null && current != null && GUILayout.Button("RESTORE STOCK 0-6-0", GUILayout.Width(200)))
                    Run(() => S060Conversion.Restore(car), "Restored " + car.ID + " to 0-6-0.");
            }

            GUILayout.Label(status);
            GUILayout.Space(6);
            GUILayout.Label(
                "0-4-0 drops the middle driver, leaving drivers 1 and 3 coupled 3.10 m apart.\n" +
                "0-8-0 adds a fourth driver behind driver 1, at the cab end, 1.549 m back: a\n" +
                "clone of driver 2's wheel plus a second coupling rod shifted one spacing.\n" +
                "Neither changes total adhesion — a tank engine has all its weight on the\n" +
                "drivers either way, so only the per-axle load changes.\n" +
                "Known cosmetic gaps: rod pin bosses double up or hang in air, the brake shoes\n" +
                "are one mesh so the fourth driver has none, and the bogie pivots stay where DV\n" +
                "put them.");
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
                Main.Logger.LogException("S060 spike action failed", ex);
            }
        }

    }
}
