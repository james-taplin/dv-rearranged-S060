using System;

namespace RearrangedS282
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

        private static void Positive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
