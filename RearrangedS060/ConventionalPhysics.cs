using System;

namespace RearrangedS060
{
    // Derailroader's G29/C21 convention: DV allocates equal weight to each axle.
    // These are parameter conversions for the native simulator, not a new force model.
    public static class ConventionalPhysics
    {
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

        // DV spreads the whole 111 t loco over its axles, so short layouts put
        // 28-37 t on each driver. Cap the driven-axle load by coupled group size
        // (single, four- and six-coupled; larger groups uncapped). Only ever
        // lowers the equal-load fraction. Articulated groups are capped per engine.
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
