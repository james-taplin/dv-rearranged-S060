namespace RearrangedS060
{
    // Authored machinery dimensions; native definitions retain float precision.
    // Meyer calibration and its existing compound/save contracts are unchanged.
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
            // .44 m outside castings accommodate .34 m bores. Both native
            // engines together retain the stock S060's 2 kg/s flow ceiling.
            internal const float Bore = .34f, Stroke = .47091934f, ChestLitres = 115, MaxFlow = 1f;
            internal const float DriverRadius = .5f, NativeDriverRadius = .712128f;
            internal const float NativeCrankRadius = Meyer.NativeCrankRadius;
            internal const float CrankRadius = NativeCrankRadius * DriverRadius / NativeDriverRadius;
            // The existing authored float expression is one ULP above the
            // runtime's literal Stroke. Preserve both contracts during cleanup;
            // merging them would be a calibration change, not an extraction.
            internal const float AuthoredStroke = 2 * CrankRadius;
        }
        internal static class Shay
        {
            // The 0.80 m wheel / 86:20 / larger-cylinder model is a new authored
            // asset revision. Do not apply the legacy R2 engine fitter to it.
            internal const float Bore = .252f, Stroke = .30080814f, ChestLitres = 45, MaxFlow = 1.6f;
            internal const float CrankRadius = .15f, RodLength = .9f, PistonAxisX = -.065f;
            internal const int RingTeeth = 86, PinionTeeth = 20;
            internal const float GearRatio = (float)RingTeeth / PinionTeeth;
            internal const float TruckShaftX = .96976744f;
            internal const float TruckShaftY = .4f;
        }
    }
}
