namespace RearrangedS060
{
    // Fixed one-metre drivers and unchanged gauge. The rear rim ends at -2.5m;
    // the front rim and full-size cylinder remain behind the native body nose.
    internal static class S060TenLayout
    {
        internal const float Diameter = 1f, Scale = Diameter / S060Selection.StockDiameter;
        internal const float Rear = -2f;
        internal const float SourceRear = S060EightLayout.NativeRear - 2 * S060EightLayout.NativePitch;
        internal static float ForwardOffset => Rear - SourceRear * Scale;
        internal static float Position(float sourceZ) => sourceZ * Scale + ForwardOffset;
    }
}
