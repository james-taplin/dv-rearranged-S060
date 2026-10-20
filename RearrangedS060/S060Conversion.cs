using System;
using System.Collections.Generic;
using UnityEngine;

namespace RearrangedS060
{
    // One conversion at a time per locomotive, applied all-or-nothing. Subclasses
    // supply the geometry; this holds the bookkeeping, the safety gate and the
    // rollback, so a half-applied drivetrain can never be left behind.
    internal abstract class S060Conversion
    {
        private static readonly Dictionary<TrainCar, S060Conversion> active =
            new Dictionary<TrainCar, S060Conversion>();

        protected S060Parts Parts { get; private set; }
        protected B99PhysicsState Physics { get; private set; }

        // The stock S060: three coupled axles, all powered.
        protected const int StockPoweredAxles = 3;

        internal abstract string Name { get; }

        protected abstract void Apply();
        protected abstract void Undo();

        internal static S060Conversion Current(TrainCar car) =>
            car != null && active.TryGetValue(car, out var state) ? state : null;

        internal static string Describe(TrainCar car)
        {
            if (car == null) return "no car";
            var state = Current(car);
            return car.ID + ": " + (state == null ? "stock 0-6-0" : state.Name);
        }

        internal static void Convert<T>(TrainCar car) where T : S060Conversion, new()
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            if (active.ContainsKey(car))
                throw new InvalidOperationException(car.ID + " is already converted; restore it first.");
            RequireStationary(car);

            var state = new T
            {
                Parts = S060Parts.Resolve(car),
                Physics = new B99PhysicsState(car, StockPoweredAxles)
            };
            try
            {
                state.Apply();
            }
            catch
            {
                try { state.Undo(); }
                catch (Exception undo) { Main.Logger.LogException("Conversion rollback failed", undo); }
                throw;
            }
            active[car] = state;
            Main.Logger.Log("S060 " + car.ID + " converted: " + state.Name);
        }

        internal static void Restore(TrainCar car)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            if (!active.TryGetValue(car, out var state))
                throw new InvalidOperationException(car.ID + " is not converted.");
            RequireStationary(car);
            state.Undo();
            active.Remove(car);
            Main.Logger.Log("S060 " + car.ID + " restored to stock 0-6-0.");
        }

        // Switching a drivetrain under load is how the S282 mod earned its
        // stationary-only rule; these are the same conditions its radio checks.
        protected static void RequireStationary(TrainCar car)
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
    }

    // Added running gear is not in the native animation clips, so it copies the
    // pose of a part that is, after the Animator has finished for the frame.
    // Coupled wheels all share a crank angle, so a wheel copies a wheel exactly;
    // a coupling rod translates without rotating, so a rod copies a rod with a
    // fixed offset along the frame.
    internal sealed class S060FollowPose : MonoBehaviour
    {
        private Transform source;
        private Vector3 offset;
        private bool followPosition;

        internal void Follow(Transform source, Vector3 offset, bool followPosition)
        {
            this.source = source;
            this.offset = offset;
            this.followPosition = followPosition;
        }

        private void LateUpdate()
        {
            if (source == null) return;
            transform.localRotation = source.localRotation;
            if (followPosition) transform.localPosition = source.localPosition + offset;
        }
    }
}
