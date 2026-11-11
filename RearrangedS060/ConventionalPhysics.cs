using System;

namespace RearrangedS060
{
    // Derailroader's G29/C21 convention: DV allocates equal weight to each axle.
    // These are parameter conversions for the native simulator, not a new force model.
    public static class ConventionalPhysics
    {
        // Fixed design datum: 41 t native dry body plus normal working supplies.
        // Radio use with a nearly empty tank must not choose a different engine
        // design. Actual mass remains native and still changes adhesion each tick.
        public const float ReferenceOperatingMassKg = 50000f;

        // An authored effective adhesive-load allowance, implemented through the
        // native friction conversion below, not through Rigidbody mass or CoM.
        // All-driven layouts keep full weight unless their coupled group reaches
        // its axle-load allowance. The 0-6-2 keeps the conservative native 3/4
        // equal-axle share pending measured suspension/load distribution.
        public static float RatedDriverWeightFraction(int coupledAxles, int totalAxles)
        {
            if (coupledAxles <= 0) throw new ArgumentOutOfRangeException(nameof(coupledAxles));
            return DriverWeightFraction(new[] { coupledAxles }, totalAxles, ReferenceOperatingMassKg);
        }

        public static float RpmPerMetrePerSecond(float radius)
        {
            Positive(radius, nameof(radius));
            return 60f / (2f * (float)Math.PI * radius);
        }

        public static float FrictionForDriverWeight(float coefficient, float driverWeightFraction, int poweredAxles, int totalAxles)
        {
            Positive(coefficient, nameof(coefficient));
            Positive(driverWeightFraction, nameof(driverWeightFraction));
            if (driverWeightFraction > 1f || poweredAxles <= 0 || totalAxles < poweredAxles || totalAxles > 255)
                throw new ArgumentOutOfRangeException(nameof(totalAxles));
            return coefficient * (driverWeightFraction / ((float)poweredAxles / totalAxles));
        }

        // Design allowances, not historical axle-load claims. A 50 t S060 on
        // only two axles would otherwise retain about 25 t of adhesive load per
        // driver. These allowances can only lower the equal-load fraction.
        public static float MaxDrivenAxleLoadKg(int coupledAxles)
        {
            if (coupledAxles == 1) return 18000f;
            if (coupledAxles == 2) return 19000f;
            if (coupledAxles == 3) return 20000f;
            return float.PositiveInfinity;
        }

        public static float DriverWeightFraction(int[] coupledGroups, int totalAxles, float locoMassKg)
        {
            Positive(locoMassKg, nameof(locoMassKg));
            int powered = 0; float cap = 0f;
            foreach (int group in coupledGroups)
            {
                if (group < 0) throw new ArgumentOutOfRangeException(nameof(coupledGroups));
                powered += group;
                cap += group * MaxDrivenAxleLoadKg(group);
            }
            if (totalAxles <= 0 || totalAxles < powered || totalAxles > 255)
                throw new ArgumentOutOfRangeException(nameof(totalAxles));
            float equal = (float)powered / totalAxles;
            return powered == 0 ? 0f : Math.Min(equal, cap / locoMassKg);
        }

        private static void Positive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
