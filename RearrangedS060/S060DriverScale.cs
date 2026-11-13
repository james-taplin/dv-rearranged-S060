using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // Running-gear layout scaling, NOT independent wheel-diameter editing.
    // Gauge is the X axis and must remain stock. The working S282 implementation
    // uses (-1, f, f)/(1, f, f), e.g. WheelRearranger.cs:2431-2432. The original
    // S060 implementation incorrectly multiplied X too; James's 0.824x test and
    // screenshot confirmed the resulting lateral misalignment on 21 Oct 2026.
    // Scaling Y/Z together preserves circular wheels and the native rod motion,
    // but also changes axle pitch and cylinder placement. That remains an
    // experimental layout change, not a completed independent resize feature.
    //
    // What it does NOT scale, because they live outside the mechanism:
    //   * the native [axle] objects under each bogie — those are repositioned to
    //     match their wheels instead, which is the caller's job;
    //   * the brake shoes and calipers, which are part of the split body. They
    //     are hidden while a non-stock scale is applied rather than left floating
    //     around wheels of the wrong size.
    //
    // CylinderFittings translates the full-size stock cylinders and every
    // associated outlet after the arrangement places its mechanisms. The upper
    // branches bend back to their fixed boiler mounts. No cylinder scaling.
    internal sealed class S060DriverScale
    {
        internal const float Stock = 1f;
        internal const float Smallest = 0.55f;
        internal const float Largest = 1.15f;

        private readonly List<(Transform mechanism, Vector3 scale)> originals =
            new List<(Transform, Vector3)>();
        private readonly List<Renderer> hiddenBrakes = new List<Renderer>();
        private readonly S060Parts parts;
        private S060CylinderFittings fittings;
        private S060FixedValveGear fixedValveGear;

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
                // Retain lateral size and the left mechanism's mirror exactly.
                mechanism.localScale = new Vector3(original.x, original.y * factor, original.z * factor);
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
            fixedValveGear?.Dispose();fixedValveGear=null;
            fittings?.Dispose(); fittings = null;
            foreach (var (mechanism, scale) in originals)
                if (mechanism != null) mechanism.localScale = scale;
            originals.Clear();
            foreach (var renderer in hiddenBrakes) if (renderer != null) renderer.enabled = true;
            hiddenBrakes.Clear();
            Factor = Stock;
        }

        // Destruction fallback: release owned meshes without touching dead
        // transforms, native physics or detached audio controller lifetimes.
        internal void ReleaseResources()
        { fixedValveGear?.ReleaseResources();fixedValveGear=null;fittings?.ReleaseResources(); fittings = null; }

        internal void PositionFittings(float forwardOffset)
        {
            if (!Mathf.Approximately(Factor, Stock) || Math.Abs(forwardOffset) > .00001f)
                fittings = S060CylinderFittings.Apply(parts, Factor, forwardOffset);
            if(!Mathf.Approximately(Factor,Stock))RetireFixedValveGear();
        }
        internal void RetireFixedValveGear()
        {if(fixedValveGear==null)fixedValveGear=S060FixedValveGear.Apply(parts.Body);}

        private IEnumerable<Renderer> BrakeRenderers() =>
            parts.Body.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name.IndexOf("brake_shoes", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            r.name.IndexOf("caliper", StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
