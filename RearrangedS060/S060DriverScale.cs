using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // Shrinks or grows the whole visible drivetrain, which is the S282 mod's own
    // trick for fitting more axles into a frame (it runs about 0.68 for the
    // 4-14-4). Both DrivingMechanism transforms sit at the car origin, at rail
    // level, so scaling them uniformly keeps every wheel on the rail, keeps the
    // rods attached to their crank pins, and takes the cylinders and valve gear
    // with it — no re-rodding, no re-timing, nothing to animate differently.
    //
    // What it does NOT scale, because they live outside the mechanism:
    //   * the native [axle] objects under each bogie — those are repositioned to
    //     match their wheels instead, which is the caller's job;
    //   * the brake shoes and calipers, which are part of the split body. They
    //     are hidden while a non-stock scale is applied rather than left floating
    //     around wheels of the wrong size.
    //
    // Scaling moves the cylinders bodily towards the car centre, because they are
    // part of the drivetrain. Moving them back out independently is what
    // DriveRetarget is for, and that is the next job rather than this one.
    internal sealed class S060DriverScale
    {
        internal const float Stock = 1f;
        internal const float Smallest = 0.55f;
        internal const float Largest = 1.15f;

        private readonly List<(Transform mechanism, Vector3 scale)> originals =
            new List<(Transform, Vector3)>();
        private readonly List<Renderer> hiddenBrakes = new List<Renderer>();
        private readonly S060Parts parts;

        internal float Factor { get; private set; } = Stock;
        internal float DriverRadius => S060Parts.DriverRadius * Factor;

        internal S060DriverScale(S060Parts parts)
        {
            this.parts = parts ?? throw new ArgumentNullException(nameof(parts));
        }

        internal void Apply(float factor)
        {
            if (float.IsNaN(factor) || factor < Smallest || factor > Largest)
                throw new ArgumentOutOfRangeException(nameof(factor),
                    "Driver scale must be between " + Smallest + " and " + Largest + ".");
            Factor = factor;
            if (Mathf.Approximately(factor, Stock)) return;

            foreach (var mechanism in new[] { parts.MechanismLeft, parts.MechanismRight })
            {
                var original = mechanism.localScale;
                originals.Add((mechanism, original));
                // DrivingMechanism_L is mirrored (x = -1), so scale component-wise
                // rather than assigning a uniform vector.
                mechanism.localScale = original * factor;
            }

            // Brake gear belongs to the body, not the drivetrain, so at any other
            // size it no longer lines up with the wheels.
            foreach (var renderer in BrakeRenderers())
            {
                if (!renderer.enabled) continue;
                renderer.enabled = false;
                hiddenBrakes.Add(renderer);
            }
        }

        internal void Restore()
        {
            foreach (var (mechanism, scale) in originals)
                if (mechanism != null) mechanism.localScale = scale;
            originals.Clear();
            foreach (var renderer in hiddenBrakes) if (renderer != null) renderer.enabled = true;
            hiddenBrakes.Clear();
            Factor = Stock;
        }

        private IEnumerable<Renderer> BrakeRenderers() =>
            parts.Body.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name.IndexOf("brake_shoes", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            r.name.IndexOf("caliper", StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
