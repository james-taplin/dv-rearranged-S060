namespace RearrangedS060
{
    // Authored simulation dimensions. Keep the cycle's existing double precision
    // and native definitions' float precision: this extraction changes no tuning.
    internal static class S060SteamCalibration
    {
        internal static class Meyer
        {
            internal const double HpBore = .42, LpBore = .594, Stroke = .6707097;
            internal const double HpChest = .2, LpChest = .4, HpFlow = 2, LpFlow = 4;
            internal const float NativeHpBore = (float)HpBore, NativeLpBore = (float)LpBore, NativeStroke = (float)Stroke;
            internal const float HpChestLitres = 200, LpChestLitres = 400;
            internal const float NativeCrankRadius = .33535485f;
#if B99_S060_ARTICULATED || B99_MEYER
            internal static MeyerCompoundCycle CreateCycle() =>
                new MeyerCompoundCycle(HpBore, LpBore, Stroke, HpChest, LpChest, HpFlow);
#endif
        }
        internal static class Dockside
        {
            internal const float Bore = .3f, Stroke = .47091934f, ChestLitres = 90, MaxFlow = .8f;
            internal const float DriverRadius = .5f, NativeDriverRadius = .712128f;
            internal const float NativeCrankRadius = Meyer.NativeCrankRadius;
            internal const float CrankRadius = NativeCrankRadius * DriverRadius / NativeDriverRadius;
            // The existing authored float expression is one ULP above the
            // runtime's literal Stroke. Preserve both contracts during cleanup;
            // merging them would be a calibration change, not an extraction.
            internal const float AuthoredStroke = 2 * CrankRadius;
        }
    }
}
