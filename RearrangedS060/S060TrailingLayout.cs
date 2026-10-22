namespace RearrangedS060
{
    // Probe-derived stock-driver layout. The pony axle is independently sized:
    // no mechanism, gauge, driver pitch or cylinder rescaling is involved.
    internal static class S060TrailingLayout
    {
        internal const float CentreZ = -2.8f;
        internal const int PoweredAxles = 3, TotalAxles = 4;
        internal const float DriverWeightFraction = 3f / 4f;
    }
}
