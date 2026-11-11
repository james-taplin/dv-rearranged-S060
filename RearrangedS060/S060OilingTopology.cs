using System;

namespace RearrangedS060
{
    // Circuit identity is simulation data, never a consequence of streamed cup
    // geometry. Original guide/crosshead/big-end slots stay 0..5; added big ends
    // occupy the same per-side slots whenever an axle is present again.
    // Two cups per driven axle, no more (James, 2026-10-28): each side runs one circuit
    // per driven axle, the native big end, guide and crosshead first, then the extras.
    internal static class S060OilingTopology
    {
        internal const int NativePoints = 6, ExtraPerSide = 4, TotalPoints = 14;

        internal static int DrivenAxles(S060Arrangement arrangement)
        {
            switch (arrangement)
            {
                case S060Arrangement.TenCoupled: return 5;
                case S060Arrangement.FourCoupled: return 2;
                case S060Arrangement.SixCoupled:
                case S060Arrangement.SixTwo: return 3;
                case S060Arrangement.EightCoupled:
                case S060Arrangement.Meyer:
                case S060Arrangement.Dockside:
                case S060Arrangement.Shay: return 4;
                default: throw new ArgumentOutOfRangeException(nameof(arrangement));
            }
        }

        internal static int NativeBigEndIndex(bool left) => left ? 2 : 5;

        // One side's live circuits, in the order cups are given to its axles.
        internal static int[] SideCircuits(S060Arrangement arrangement, bool left)
        {
            int axles = DrivenAxles(arrangement);
            var circuits = new System.Collections.Generic.List<int> { NativeBigEndIndex(left), left ? 0 : 3, left ? 1 : 4 };
            for (int slot = 0; circuits.Count < axles; slot++) circuits.Add(ExtraIndex(left, slot));
            return circuits.GetRange(0, axles).ToArray();
        }

        internal static int ExtraIndex(bool left, int slot)
        {
            if (slot < 0 || slot >= ExtraPerSide) throw new ArgumentOutOfRangeException(nameof(slot));
            // Preserve all twelve existing save/port indexes. Fifth-axle cups append.
            return slot == 3 ? (left ? 12 : 13) : NativePoints + (left ? 0 : 3) + slot;
        }

        internal static bool[] ActiveMask(S060Selection selection)
        {
            var active = new bool[TotalPoints];
            // Shay R2: six upright cups serve exactly the six native circuits; extras stay dormant.
            if (selection.Arrangement == S060Arrangement.Shay) { for (int i = 0; i < NativePoints; i++) active[i] = true; return active; }
            foreach (bool left in new[] { true, false })
                foreach (int index in SideCircuits(selection.Arrangement, left)) active[index] = true;
            return active;
        }
    }
}
