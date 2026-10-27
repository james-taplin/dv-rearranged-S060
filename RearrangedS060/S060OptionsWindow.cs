#if B99_S060_FUNCTIONAL
using UnityEngine;

namespace RearrangedS060
{
    internal static class S060OptionsWindow
    {
        private static string status = "Use S060 WHEEL REARRANGER on the comms radio, or the buttons below.";

        internal static void Draw()
        {
            GUILayout.Label("Rearranged S060 — radio and saved arrangements");
            GUILayout.Label("Stop and close the regulator before changing running gear. Each arrangement has fitted wheels and geometry.");
            Main.Options.Draw();
            GUILayout.Space(10);
            var car = S060Targeting.Target();
            GUILayout.Label(car == null ? "No S060 in view or within 30 m." : "Target: " + S060Conversion.Describe(car));
            if (car != null)
            {
                var owner = car.GetComponent<S060Lifecycle>();
                if (!string.IsNullOrEmpty(owner?.Status)) GUILayout.Label(owner.Status);
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("APPLY 0-4-0")) Apply(car, S060Arrangement.FourCoupled);
                    if (GUILayout.Button("0-6-0 SHORT WHEELBASE")) Apply(car, S060Arrangement.SixCoupled);
                    if (GUILayout.Button("APPLY 0-8-0")) Apply(car, S060Arrangement.EightCoupled);
                    if (GUILayout.Button("APPLY 0-10-0")) Apply(car, S060Arrangement.TenCoupled);
                    if (GUILayout.Button("APPLY 0-6-2")) Apply(car, S060Arrangement.SixTwo);
                    if (GUILayout.Button("RESTORE STOCK"))
                        S060Lifecycle.TryApply(car, S060Selection.Stock, out status);
                }
#if B99_S060_ARTICULATED
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("APPLY SHAY")) Apply(car, S060Arrangement.Shay);
                    if (GUILayout.Button("APPLY MEYER COMPOUND")) Apply(car, S060Arrangement.Meyer);
                    if (GUILayout.Button("APPLY FRANKLIN DOCKSIDE")) Apply(car, S060Arrangement.Dockside);
                }
#endif
            }
            GUILayout.Label(status);
            GUILayout.Label("The short-wheelbase 0-6-0 and 0-8-0 share 1.14 m drivers. Stock restoration returns the original locomotive geometry.");
        }

        private static void Apply(TrainCar car, S060Arrangement arrangement) =>
            S060Lifecycle.TryApply(car, S060Selection.ForConversion(arrangement, Main.Options.ValidScale), out status);

    }
}
#endif
