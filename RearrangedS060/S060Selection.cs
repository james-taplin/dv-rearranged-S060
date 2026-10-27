using System;
using System.Globalization;
using System.Collections.Generic;

namespace RearrangedS060
{
    // Stable S060-only IDs; independent of the inherited S282 catalogue.
    internal enum S060Arrangement { SixCoupled = 0, FourCoupled = 1, EightCoupled = 2, SixTwo = 3, Meyer = 4, Dockside = 5, TenCoupled = 6, Shay = 7 }

    internal readonly struct S060Selection : IEquatable<S060Selection>
    {
        internal const float MinimumScale = .55f, MaximumScale = 1.15f;
        internal const float StockDiameter = 1.424256f;
        internal const float SmallSixScale = S060EightLayout.MaximumScale;
        internal S060Arrangement Arrangement { get; }
        internal float Scale { get; }
        internal static S060Selection Stock => new S060Selection(S060Arrangement.SixCoupled, 1f);
        // Release choices have authored sizes. The second 0-6-0 option is
        // short-wheelbase; explicit stock is only the separate Stock selection.
        internal static S060Selection ForConversion(S060Arrangement arrangement, float scale) =>
            new S060Selection(arrangement, arrangement == S060Arrangement.SixCoupled ? SmallSixScale : 1f);
        internal bool IsStock => Arrangement == S060Arrangement.SixCoupled && Scale == 1f;
        internal string Name => Arrangement == S060Arrangement.SixCoupled && !IsStock ? "0-6-0 Short Wheelbase" : NameOf(Arrangement);
        internal float DriverDiameter => Arrangement == S060Arrangement.Shay ? StockDiameter * .8f : (Arrangement == S060Arrangement.Dockside || Arrangement == S060Arrangement.TenCoupled) ? 1f : StockDiameter * Scale;
        internal bool FixedSize => RequiresFixedSize(Arrangement);
        internal string Description => Name + " / " + DriverDiameter.ToString("0.00", CultureInfo.InvariantCulture) + " m drivers";

        internal S060Selection(S060Arrangement arrangement, float scale)
        {
            if (!IsSupported((int)arrangement)) throw new ArgumentOutOfRangeException(nameof(arrangement));
            if (!IsValidScale(scale) || (RequiresFixedSize(arrangement) && scale != 1f))
                throw new ArgumentOutOfRangeException(nameof(scale), "This layout uses its authored wheel size.");
            Arrangement = arrangement;
            Scale = arrangement == S060Arrangement.EightCoupled ? S060EightLayout.FitScale(scale) :
                arrangement == S060Arrangement.FourCoupled ? 1f :
                arrangement == S060Arrangement.SixCoupled && scale != 1f ? SmallSixScale : scale;
        }

        internal static bool IsSupported(int id) => id >= 0 && id <=
#if B99_S060_ARTICULATED
            7;
#else
            3;
#endif
        internal static bool RequiresFixedSize(S060Arrangement arrangement) => arrangement == S060Arrangement.Shay || arrangement == S060Arrangement.TenCoupled || arrangement == S060Arrangement.SixTwo || arrangement == S060Arrangement.Meyer || arrangement == S060Arrangement.Dockside;
        internal static bool IsValidScale(float scale) => !float.IsNaN(scale) && !float.IsInfinity(scale) &&
            scale >= MinimumScale && scale <= MaximumScale;
        internal static string NameOf(S060Arrangement arrangement) => arrangement == S060Arrangement.FourCoupled
            ? "0-4-0" : arrangement == S060Arrangement.EightCoupled ? "0-8-0" : arrangement == S060Arrangement.SixTwo ? "0-6-2" :
            arrangement == S060Arrangement.Shay ? "Two-truck Shay" : arrangement == S060Arrangement.TenCoupled ? "0-10-0 Tank" : arrangement == S060Arrangement.Meyer ? "0-4+4-0T Meyer Compound" : arrangement == S060Arrangement.Dockside ? "0-4-4-0T Franklin Dockside" : "0-6-0";
        internal static S060Selection PickInitial(bool randomize, bool six, bool four, bool eight, float scale, Random random, bool trailing = false, bool meyer = false, bool dockside = false, bool ten = false)
        {
            if (!randomize) return Stock;
            var pool = new List<S060Arrangement>();
            if (six) pool.Add(S060Arrangement.SixCoupled);
            if (four) pool.Add(S060Arrangement.FourCoupled);
            if (eight) pool.Add(S060Arrangement.EightCoupled);
            if (trailing) pool.Add(S060Arrangement.SixTwo);
#if B99_S060_ARTICULATED
            if (meyer) pool.Add(S060Arrangement.Meyer);
            if (dockside) pool.Add(S060Arrangement.Dockside);
            if (ten) pool.Add(S060Arrangement.TenCoupled);
#endif
            if (pool.Count == 0) return Stock;
            var selected = pool[random.Next(pool.Count)];
            return new S060Selection(selected, 1f);
        }
        public bool Equals(S060Selection other) => Arrangement == other.Arrangement && Scale == other.Scale;
        public override bool Equals(object obj) => obj is S060Selection other && Equals(other);
        public override int GetHashCode() => ((int)Arrangement * 397) ^ Scale.GetHashCode();
    }
}
