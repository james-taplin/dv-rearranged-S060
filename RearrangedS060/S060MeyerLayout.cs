using UnityEngine;

namespace RearrangedS060
{
    // Engine presentation only. Native bogie starting transforms, the R2 body,
    // buffers/coupler/lamp datum, engine IDs and saved state remain unchanged.
    internal static class S060MeyerLayout
    {
        internal const float FrontVisualYaw = 180f;
        // Inward cylinders sit clear of the adjacent native driver flange.
        // Cylinder and main-rod geometry change together; wheels/steam calibration do not.
        internal const float FrontCylinderShift = .85f;
        internal const float FrontCylinderZ = MeyerPlan.CylinderZ + FrontCylinderShift;
        internal const float FrontMainRodLength = MeyerPlan.MainRodLength + FrontCylinderShift;
        // Fixed R2 rear beam limits an aft move: the native driver sweep at
        // yaw +/-6, pitch/roll +/-1 and +/-20 mm retains >15 mm at this offset.
        internal const float RearVisualOffsetZ = -.065f;
        internal static Quaternion FrontRotation => Quaternion.Euler(0, FrontVisualYaw, 0);
        internal static Vector3 RearVisualOffset => new Vector3(0, 0, RearVisualOffsetZ);
    }
}
