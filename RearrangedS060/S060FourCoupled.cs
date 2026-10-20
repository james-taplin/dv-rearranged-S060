using System;
using System.Collections.Generic;
using System.Linq;
using LocoSim.Implementations.Wheels;
using UnityEngine;

namespace RearrangedS060
{
    // P1 spike: turn a stationary S060 into an 0-4-0 and back again.
    //
    // Driver 2, the middle one, is the one that goes. Two measured reasons:
    //   * Axle_R owns only driver 1, so taking driver 1 would leave a bogie with
    //     no axles at all;
    //   * the main rod runs forward from driver 3 (valve gear reaches Z +3.46),
    //     so driver 3 has to stay to keep the gear driven.
    // What is left is a long-wheelbase 0-4-0 on drivers 1 and 3, 3.10 m apart,
    // and the stock three-pin coupling rod still joins them correctly — its
    // middle pin boss is simply left hanging in air, which is a known cosmetic
    // flaw of this spike rather than an oversight.
    //
    // Deliberately NOT done here: the bogie pivots are left exactly where DV put
    // them (+0.929 and -1.396). The car therefore rides and curves precisely as
    // stock, and only the wheel count, the powered-axle registration and the
    // adhesion maths change. Whether the offset between a pivot and its one
    // remaining axle looks wrong in motion is the main thing the in-game test has
    // to answer.
    internal sealed class S060FourCoupled
    {
        private const int DroppedDriver = 2;
        private const int StockPoweredAxles = 3;

        private static readonly Dictionary<TrainCar, S060FourCoupled> active =
            new Dictionary<TrainCar, S060FourCoupled>();

        private readonly S060Parts parts;
        private readonly B99PhysicsState physics;
        private readonly List<Renderer> hiddenCalipers = new List<Renderer>();
        private readonly List<Transform> hiddenWheels = new List<Transform>();
        private Transform droppedAxle;

        private S060FourCoupled(S060Parts parts, B99PhysicsState physics)
        {
            this.parts = parts;
            this.physics = physics;
        }

        internal static bool IsConverted(TrainCar car) => car != null && active.ContainsKey(car);

        internal static string Describe(TrainCar car)
        {
            if (car == null) return "no car";
            return car.ID + (IsConverted(car) ? ": 0-4-0 (drivers 1 and 3)" : ": stock 0-6-0");
        }

        internal static void Convert(TrainCar car)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            if (active.ContainsKey(car)) throw new InvalidOperationException(car.ID + " is already an 0-4-0.");
            RequireStationary(car);

            var parts = S060Parts.Resolve(car);
            var physics = new B99PhysicsState(car, StockPoweredAxles);
            var state = new S060FourCoupled(parts, physics);
            try
            {
                state.Apply();
            }
            catch
            {
                // A half-applied drivetrain is worse than none: undo and rethrow so
                // the caller reports the original failure.
                try { state.Undo(); } catch (Exception undo) { Main.Logger.LogException("0-4-0 rollback failed", undo); }
                throw;
            }
            active[car] = state;
            Main.Logger.Log("S060 " + car.ID + " converted to 0-4-0: drivers 1 and 3 at car-local Z " +
                S060Parts.Driver1Z.ToString("0.000") + " and " + S060Parts.Driver3Z.ToString("0.000") +
                ", driver " + DroppedDriver + " hidden, 2 powered axles registered.");
        }

        internal static void Restore(TrainCar car)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            if (!active.TryGetValue(car, out var state)) throw new InvalidOperationException(car.ID + " is not converted.");
            RequireStationary(car);
            state.Undo();
            active.Remove(car);
            Main.Logger.Log("S060 " + car.ID + " restored to stock 0-6-0.");
        }

        // Switching a drivetrain under load is how the S282 mod earned its
        // stationary-only rule; the regulator check is the same one its radio makes.
        private static void RequireStationary(TrainCar car)
        {
            if (car.derailed || car.isExploded)
                throw new InvalidOperationException("Use a repaired, rerailed locomotive.");
            float speed = Mathf.Abs(car.GetForwardSpeed());
            if (speed > 0.01f)
                throw new InvalidOperationException("Stop the locomotive first (moving at " +
                    speed.ToString("0.00") + " m/s).");
            var sim = car.SimController;
            if (sim?.simFlow == null)
                throw new InvalidOperationException("The native simulation is not ready yet.");
            if (sim.simFlow.TryGetPort("throttle.EXT_IN", out var throttle) && throttle.Value > 0.001f)
                throw new InvalidOperationException("Close the regulator first.");
            if (sim.drivingForce != null && Mathf.Abs(sim.drivingForce.generatedForce) > 500f)
                throw new InvalidOperationException("Wait for the driving force to settle.");
            if (sim.wheelslipController != null && sim.wheelslipController.IsWheelslipping)
                throw new InvalidOperationException("Wait for the wheelslip to stop.");
            if (car.adhesionController != null && car.adhesionController.IsWheelSliding)
                throw new InvalidOperationException("Wait for the wheel slide to stop.");
        }

        private void Apply()
        {
            // 1. Visuals: both sides' middle wheel.
            foreach (var wheel in parts.Wheels(DroppedDriver))
            {
                wheel.gameObject.SetActive(false);
                hiddenWheels.Add(wheel);
            }

            // 2. That axle's brake caliper, which the splitter has already separated.
            foreach (var caliper in parts.CaliperRenderers(DroppedDriver))
            {
                if (!caliper.enabled) continue;
                caliper.enabled = false;
                hiddenCalipers.Add(caliper);
            }

            // 3. The physics axle. BogieAxlesPatch drops inactive axles from the
            //    rebuilt list, and nulling the field is what forces that rebuild.
            droppedAxle = parts.AxleFor(DroppedDriver)
                ?? throw new InvalidOperationException("No [axle] maps to driver " + DroppedDriver + ".");
            droppedAxle.gameObject.SetActive(false);
            parts.BogieFor(DroppedDriver).axles = null;

            // 4. Simulation: two powered axles out of two total, all the weight on
            //    them because a tank engine carries nothing else.
            var powered = new[] { parts.PoweredFor(1), parts.PoweredFor(3) };
            if (powered.Any(p => p == null))
                throw new InvalidOperationException("Drivers 1 and 3 must both keep a PoweredWheel.");
            physics.Apply(S060Parts.DriverRadius, powered, totalAxles: 2, driverWeightFraction: 1f);
        }

        private void Undo()
        {
            try { physics.Restore(); }
            catch (Exception ex) { Main.Logger.LogException("Restoring S060 physics failed", ex); }

            if (droppedAxle != null)
            {
                droppedAxle.gameObject.SetActive(true);
                parts.BogieFor(DroppedDriver).axles = null;
                droppedAxle = null;
            }
            foreach (var caliper in hiddenCalipers) if (caliper != null) caliper.enabled = true;
            hiddenCalipers.Clear();
            foreach (var wheel in hiddenWheels) if (wheel != null) wheel.gameObject.SetActive(true);
            hiddenWheels.Clear();
        }
    }
}
