using System;

namespace RearrangedS060
{
    // The stock 0-6-0 at a different driver size and nothing else: no wheels
    // added or dropped, no rods cloned. It exists so the scale itself can be
    // judged on its own, without an unfamiliar wheel arrangement confusing the
    // picture, and because a small-wheeled 0-6-0 shunter is a perfectly
    // reasonable thing to want.
    internal sealed class S060Rescaled : S060Conversion
    {
        internal override string Name =>
            "0-6-0 rescaled to " + (2f * Scale.DriverRadius).ToString("0.00") + " m drivers";

        protected override void Apply()
        {
            if (Math.Abs(Scale.Factor - S060DriverScale.Stock) < 0.001f)
                throw new InvalidOperationException(
                    "Move the driver size slider first — at stock size this changes nothing.");

            foreach (int driver in new[] { 1, 2, 3 })
                AxlePairs.Add((Parts.AxleFor(driver), Parts.RightWheel(driver)));

            var powered = new[] { Parts.PoweredFor(1), Parts.PoweredFor(2), Parts.PoweredFor(3) };
            // Same three axles, same weight: only the rolling radius changes, which
            // is what turns wheel size into speed and tractive effort.
            Physics.Apply(Scale.DriverRadius, powered, totalAxles: 3, driverWeightFraction: 1f);
        }

        protected override void Undo()
        {
            try { Physics.Restore(); }
            catch (Exception ex) { Main.Logger.LogException("Restoring S060 physics failed", ex); }
        }
    }
}
