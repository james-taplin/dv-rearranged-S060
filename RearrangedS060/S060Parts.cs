using System;
using System.Collections.Generic;
using System.Linq;
using LocoSim.Implementations.Wheels;
using UnityEngine;

namespace RearrangedS060
{
    // Everything the S060 geometry work needs to find on a car, in one place, with
    // the measured facts that justify each lookup. Figures come from the P0 probe
    // report (docs/FEASIBILITY.md section 1); nothing here is guessed.
    //
    // The S060 differs from the S282 in two ways that break the inherited lookups:
    //   * its axle objects are named plainly "[axle]", never "[axle] drive N", so
    //     they are identified by distanceFromBogiePivot instead;
    //   * the three axles split 2 + 1 across Axle_F / Axle_R, with the SINGLE axle
    //     on Axle_R at the cab end, so Axle_R cannot give one up.
    internal sealed class S060Parts
    {
        // Visual driver Z centres, car-local, probe report lines 32-34.
        internal const float Driver1Z = -1.3978f;   // rearmost, cab end, owned by Axle_R
        internal const float Driver2Z = 0.1498f;    // middle, owned by Axle_F
        internal const float Driver3Z = 1.7006f;    // front, owned by Axle_F, takes the main rod
        // Rolling radius read off the PoweredWheel axle height, probe line 1605.
        internal const float DriverRadius = 0.712128f;

        internal TrainCar Car { get; private set; }
        internal Transform Body { get; private set; }
        internal Transform MechanismLeft { get; private set; }
        internal Transform MechanismRight { get; private set; }
        internal Bogie FrontBogie { get; private set; }
        internal Bogie RearBogie { get; private set; }

        // Index 1..3 as in the mesh names, both sides.
        private readonly Dictionary<int, List<Transform>> wheels = new Dictionary<int, List<Transform>>();
        // The [axle] transform that drives each visual wheel, where there is one.
        private readonly Dictionary<int, Transform> axleForWheel = new Dictionary<int, Transform>();
        private readonly Dictionary<int, PoweredWheel> poweredForWheel = new Dictionary<int, PoweredWheel>();

        internal IEnumerable<Transform> Wheels(int index) =>
            wheels.TryGetValue(index, out var list) ? list : Enumerable.Empty<Transform>();

        internal Transform AxleFor(int index) =>
            axleForWheel.TryGetValue(index, out var axle) ? axle : null;

        internal PoweredWheel PoweredFor(int index) =>
            poweredForWheel.TryGetValue(index, out var powered) ? powered : null;

        internal Bogie BogieFor(int index) =>
            index == 1 ? RearBogie : FrontBogie;

        internal static bool IsS060(TrainCar car)
        {
            if (car == null) return false;
            // Match on the type name rather than a hard-coded enum member so this
            // keeps working if the enum is renumbered; the livery is the fallback.
            if (car.carType.ToString().IndexOf("S060", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            var livery = car.carLivery;
            return livery != null && livery.id != null &&
                   livery.id.IndexOf("S060", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static S060Parts Resolve(TrainCar car)
        {
            if (!IsS060(car)) throw new InvalidOperationException("That car is not an S060.");
            var parts = new S060Parts { Car = car };

            parts.Body = car.transform.Find("LocoS060_Body")
                ?? throw new InvalidOperationException("LocoS060_Body was not found.");
            parts.MechanismLeft = parts.Body.Find("DrivingMechanism_L")
                ?? throw new InvalidOperationException("DrivingMechanism_L was not found.");
            parts.MechanismRight = parts.Body.Find("DrivingMechanism_R")
                ?? throw new InvalidOperationException("DrivingMechanism_R was not found.");

            for (int index = 1; index <= 3; index++)
            {
                string name = "s060_Wheels_0" + index;
                var found = new[] { parts.MechanismLeft, parts.MechanismRight }
                    .Select(side => side.Find(name))
                    .Where(t => t != null)
                    .ToList();
                if (found.Count != 2)
                    throw new InvalidOperationException(name + ": expected one wheel per side, found " + found.Count + ".");
                parts.wheels[index] = found;
            }

            var bogies = car.Bogies;
            if (bogies == null || bogies.Length != 2)
                throw new InvalidOperationException("Expected two bogies, found " + (bogies?.Length ?? 0) + ".");
            parts.FrontBogie = bogies.FirstOrDefault(b => b.isFrontBogie)
                ?? throw new InvalidOperationException("No front bogie.");
            parts.RearBogie = bogies.FirstOrDefault(b => !b.isFrontBogie)
                ?? throw new InvalidOperationException("No rear bogie.");

            // Map axles to visual wheels by car-local Z, which is the only link the
            // S060's naming leaves us. 0.25 m is a quarter of the 1.55 m spacing, so
            // a mismatch cannot be silently absorbed.
            foreach (var bogie in bogies)
            {
                foreach (var axle in bogie.Axles)
                {
                    if (axle.transform == null) continue;
                    float z = car.transform.InverseTransformPoint(axle.transform.position).z;
                    int index = NearestDriver(z);
                    if (index == 0)
                        throw new InvalidOperationException("An [axle] at car-local Z " +
                            z.ToString("0.000") + " matches no known driver position.");
                    if (parts.axleForWheel.ContainsKey(index))
                        throw new InvalidOperationException("Two axles claim driver " + index + ".");
                    parts.axleForWheel[index] = axle.transform;

                    var powered = axle.transform.GetComponent<PoweredWheel>();
                    if (powered != null) parts.poweredForWheel[index] = powered;
                }
            }
            if (parts.axleForWheel.Count != 3)
                throw new InvalidOperationException("Expected three axles, mapped " + parts.axleForWheel.Count + ".");
            if (parts.poweredForWheel.Count != 3)
                throw new InvalidOperationException("Expected three PoweredWheels, mapped " + parts.poweredForWheel.Count + ".");
            return parts;
        }

        private static int NearestDriver(float z)
        {
            if (Mathf.Abs(z - Driver1Z) < 0.25f) return 1;
            if (Mathf.Abs(z - Driver2Z) < 0.25f) return 2;
            if (Mathf.Abs(z - Driver3Z) < 0.25f) return 3;
            return 0;
        }

        // LocoMeshSplitter has already split the body, so each axle's brake caliper
        // is its own renderer: "s060_caliper_l_2" / "s060_caliper_r_2" under the
        // split-body clones (probe report lines 117 and 170). The brake SHOES remain
        // one mesh spanning all three axles and cannot be hidden per axle.
        internal IEnumerable<Renderer> CaliperRenderers(int index)
        {
            string left = "s060_caliper_l_" + index;
            string right = "s060_caliper_r_" + index;
            return Body.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name == left || r.name == right);
        }
    }
}
