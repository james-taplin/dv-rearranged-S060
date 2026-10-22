using System;

namespace RearrangedS060
{
    internal static class S060WheelSlots
    {
        // Native S060 wheel identities are 0..2; the added axle is 3. Reused
        // native wheels retain their keys; newly authored units use free keys.
        internal static int[] Map(int[] originalIndexes)
        {
            if (originalIndexes == null || originalIndexes.Length > 4) throw new ArgumentException("Invalid S060 wheel count.");
            var used = new bool[4]; var result = (int[])originalIndexes.Clone();
            foreach (int id in result)
            {
                if (id == -1) continue;
                if (id < 0 || id > 2 || used[id]) throw new ArgumentException("Invalid or repeated native S060 wheel.");
                used[id] = true;
            }
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != -1) continue;
                int slot = Array.FindIndex(used, value => !value);
                if (slot < 0) throw new ArgumentException("No S060 wheel identity available.");
                used[slot] = true; result[i] = slot;
            }
            return result;
        }
        internal static bool Valid(int[] states) => states != null && states.Length == 4 && Array.TrueForAll(states, value => value >= 0 && value <= 2);
    }
}
