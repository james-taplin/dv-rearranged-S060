using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // 0-4-0: drop the middle driver, leaving drivers 1 and 3 coupled 3.10 m apart.
    //
    // Driver 2 is the one that goes, for two measured reasons: Axle_R owns only
    // driver 1, so taking driver 1 would leave a bogie with no axles, and the native
    // main-rod crank is on driver 1 (probe pivot follows its measured pin),
    // so driver 1 must remain to keep the gear driven. The native coupling-rod
    // animation stays in place, with an owned two-boss
    // mesh fitted to drivers 1 and 3; no unsupported middle boss remains.
    //
    // The bogie pivots are deliberately left where DV put them (+0.929, -1.396).
    // Proven in game on 20 Oct 2026: the car rides and curves as stock.
    internal sealed class S060FourCoupled : S060Conversion
    {
        private const int DroppedDriver = 2;

        private readonly List<Renderer> hiddenCalipers = new List<Renderer>();
        private readonly List<Transform> hiddenWheels = new List<Transform>();
        private Transform droppedAxle;

        internal override string Name => "0-4-0 (drivers 1 and 3)";

        protected override void Apply()
        {
            foreach (var wheel in Parts.Wheels(DroppedDriver))
            {
                wheel.gameObject.SetActive(false);
                hiddenWheels.Add(wheel);
            }

            // That axle's brake caliper, which LocoMeshSplitter has already separated.
            foreach (var caliper in Parts.CaliperRenderers(DroppedDriver))
            {
                if (!caliper.enabled) continue;
                caliper.enabled = false;
                hiddenCalipers.Add(caliper);
            }

            // BogieAxlesPatch drops inactive axles from the rebuilt list, and nulling
            // the field is what forces that rebuild.
            droppedAxle = Parts.AxleFor(DroppedDriver)
                ?? throw new InvalidOperationException("No [axle] maps to driver " + DroppedDriver + ".");
            droppedAxle.gameObject.SetActive(false);
            Parts.BogieFor(DroppedDriver).axles = null;

            // Two physical powered axles remain. Use the authored 38 t effective
            // adhesive allowance at the fixed 50 t design datum, rather than
            // granting stock grip to two overloaded driver axles. Native mass,
            // brakes and suspension are not changed by this design allowance.
            var powered = new[] { Parts.PoweredFor(1), Parts.PoweredFor(3) };
            if (powered.Any(p => p == null))
                throw new InvalidOperationException("Drivers 1 and 3 must both keep a PoweredWheel.");
            foreach (int driver in new[] { 1, 3 })
                AxlePairs.Add((Parts.AxleFor(driver), Parts.RightWheel(driver)));
            Physics.Apply(Scale.DriverRadius, powered, totalAxles: 2,
                driverWeightFraction: ConventionalPhysics.RatedDriverWeightFraction(2, 2));
        }

        protected override void Undo()
        {
            try { Physics.Restore(); }
            finally
            {
                if (droppedAxle != null)
                {
                    droppedAxle.gameObject.SetActive(true);
                    Parts.BogieFor(DroppedDriver).axles = null;
                    droppedAxle = null;
                }
                foreach (var caliper in hiddenCalipers) if (caliper != null) caliper.enabled = true;
                hiddenCalipers.Clear();
                foreach (var wheel in hiddenWheels) if (wheel != null) wheel.gameObject.SetActive(true);
                hiddenWheels.Clear();
            }
        }
    }
}
