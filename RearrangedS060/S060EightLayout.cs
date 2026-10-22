using System;

namespace RearrangedS060
{
    // Keep the rearmost axle at the stock rear-driver station, rather than
    // hanging a fourth full-size wheel beneath the centre of the cab.
    internal static class S060EightLayout
    {
        internal const float MaximumScale = .80f;
        internal const float NativePitch = 1.5493f;
        internal const float NativeRear = -1.3978f;
        internal const float NativeAddedRear = NativeRear - NativePitch;
        internal const float NativeFront = 1.7006f;
        internal const float NativeGearFront = 3.460f;
        internal const float CouplerInnerFace = 3.470f;
        internal const float BodyFront = 4.0921f;
        internal static float FitScale(float requested)
        {
            if (float.IsNaN(requested) || float.IsInfinity(requested) || requested < .55f || requested > 1.15f)
                throw new ArgumentOutOfRangeException(nameof(requested));
            return MaximumScale; // authored arrangement; old slider values migrate here
        }
        internal static float ForwardOffset(float scale) => NativeRear - NativeAddedRear * scale;
        internal static float Position(float sourceZ, float scale) => sourceZ * scale + ForwardOffset(scale);
    }
}
