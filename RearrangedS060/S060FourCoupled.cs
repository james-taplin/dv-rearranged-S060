using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // 0-4-0: drop the middle driver, leaving drivers 1 and 3 coupled 3.10 m apart.
    //
    // Driver 2 is the one that goes, for two measured reasons: Axle_R owns only
    // driver 1, so taking driver 1 would leave a bogie with no axles, and the main
    // rod runs forward from driver 3, so driver 3 has to stay to keep the gear
    // driven. The stock three-pin coupling rod still joins 1 and 3 correctly; its
    // middle pin boss is left hanging in air, a known cosmetic flaw of this spike.
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

            // Two powered axles out of two, all the weight on them: a tank engine
            // carries nothing that is not on a driver, so total adhesion is
            // unchanged from stock and only the per-axle load doubles.
            var powered = new[] { Parts.PoweredFor(1), Parts.PoweredFor(3) };
            if (powered.Any(p => p == null))
                throw new InvalidOperationException("Drivers 1 and 3 must both keep a PoweredWheel.");
            foreach (int driver in new[] { 1, 3 })
                AxlePairs.Add((Parts.AxleFor(driver), Parts.RightWheel(driver)));
            Physics.Apply(Scale.DriverRadius, powered, totalAxles: 2, driverWeightFraction: 1f);
        }

        protected override void Undo()
        {
            try { Physics.Restore(); }
            catch (Exception ex) { Main.Logger.LogException("Restoring S060 physics failed", ex); }

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
