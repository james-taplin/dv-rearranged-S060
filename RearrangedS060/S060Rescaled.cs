using System;

namespace RearrangedS060
{
    // Fixed short-wheelbase 0-6-0: the same fitted wheels/pitch as the 0-8-0,
    // three powered axles, full-size translated cylinders and joined pipes.
    internal sealed class S060Rescaled : S060Conversion
    {
        internal override string Name =>
            "0-6-0 Short Wheelbase (" + (2f * Scale.DriverRadius).ToString("0.00") + " m drivers)";

        protected override void Apply()
        {
            if (Math.Abs(Scale.Factor - S060DriverScale.Stock) < 0.001f)
                throw new InvalidOperationException(
                    "The short-wheelbase arrangement requires its fitted driver geometry.");

            foreach (int driver in new[] { 1, 2, 3 })
                AxlePairs.Add((Parts.AxleFor(driver), Parts.RightWheel(driver)));

            var powered = new[] { Parts.PoweredFor(1), Parts.PoweredFor(2), Parts.PoweredFor(3) };
            // Same three axles, same weight: only the rolling radius changes, which
            // is what turns wheel size into speed and tractive effort.
            Physics.Apply(Scale.DriverRadius, powered, totalAxles: 3,
                driverWeightFraction: ConventionalPhysics.RatedDriverWeightFraction(3, 3));
        }

        protected override void Undo()
        {
            Physics.Restore();
        }
    }
}
