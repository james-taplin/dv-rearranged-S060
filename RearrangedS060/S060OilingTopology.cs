using System;

namespace RearrangedS060
{
    // Circuit identity is simulation data, never a consequence of streamed cup
    // geometry. Original guide/crosshead/big-end slots stay 0..5; added big ends
    // occupy the same per-side slots whenever an axle is present again.
    internal static class S060OilingTopology
    {
        internal const int NativePoints = 6, ExtraPerSide = 3, TotalPoints = 12;

        internal static int DrivenAxles(S060Arrangement arrangement)
        {
            switch (arrangement)
            {
                case S060Arrangement.FourCoupled: return 2;
                case S060Arrangement.SixCoupled:
                case S060Arrangement.SixTwo: return 3;
                case S060Arrangement.EightCoupled:
                case S060Arrangement.Meyer:
                case S060Arrangement.Dockside: return 4;
                default: throw new ArgumentOutOfRangeException(nameof(arrangement));
            }
        }

        internal static int NativeBigEndIndex(bool left) => left ? 2 : 5;

        internal static int ExtraIndex(bool left, int slot)
        {
            if (slot < 0 || slot >= ExtraPerSide) throw new ArgumentOutOfRangeException(nameof(slot));
            return NativePoints + (left ? 0 : ExtraPerSide) + slot;
        }

        internal static bool[] ActiveMask(S060Selection selection)
        {
            var active = new bool[TotalPoints];
            for (int i = 0; i < NativePoints; i++) active[i] = true;
            int extra = DrivenAxles(selection.Arrangement) - 1;
            for (int slot = 0; slot < extra; slot++)
                active[ExtraIndex(true, slot)] = active[ExtraIndex(false, slot)] = true;
            return active;
        }
    }
}
