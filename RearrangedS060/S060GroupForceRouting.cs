#if B99_S060_ARTICULATED
// Force allocation only: callers own adhesion limits, rigidbodies and lifecycle.
using System;

namespace RearrangedS060
{
    internal static class S060GroupForceRouting
    {
        // A group may span multiple native bogies; groups may share a bogie.
        // axleCounts[g,b] records that group's physical driven axles on that
        // bogie. Exactly one input force is apportioned among those contacts.
        public static float[] Distribute(float[] forces, int[,] axleCounts)
        {
            if (forces == null || axleCounts == null || axleCounts.GetLength(0) != forces.Length || axleCounts.GetLength(1) == 0)
                throw new ArgumentException("Group/bogie dimensions do not match.");
            var output = new float[axleCounts.GetLength(1)];
            DistributeInto(forces, axleCounts, output);
            return output;
        }

        // Same operation order as Distribute, with a caller-owned output buffer.
        public static void DistributeInto(float[] forces, int[,] axleCounts, float[] output)
        {
            if (forces == null || axleCounts == null || output == null || ReferenceEquals(forces, output) ||
                axleCounts.GetLength(0) != forces.Length || axleCounts.GetLength(1) != output.Length || output.Length == 0)
                throw new ArgumentException("Group/bogie dimensions do not match.");
            Array.Clear(output, 0, output.Length);
            for (int group = 0; group < forces.Length; group++)
            {
                float force = forces[group];
                if (float.IsNaN(force) || float.IsInfinity(force)) throw new ArgumentException("Non-finite force.");
                int count = 0;
                for (int bogie = 0; bogie < output.Length; bogie++)
                {
                    int n = axleCounts[group, bogie];
                    if (n < 0) throw new ArgumentException("Negative axle count.");
                    count = checked(count + n);
                }
                if (count == 0) throw new ArgumentException("A powered group has no contact bogie.");
                for (int bogie = 0; bogie < output.Length; bogie++)
                    output[bogie] += force * ((float)axleCounts[group, bogie] / count);
            }
        }
    }
}

#endif
