using UnityEngine;

namespace RearrangedS060
{
    // Shared by live mounting and reopened-bundle sweep audits. Native cup
    // origins are above the threaded stem, not at the bottom of the mesh.
    public static class S060OilCupGeometry
    {
        public const float CrankRadius = .33535485f, ThreadEmbed = .006f;
        public const float CouplingRodPlaneX = .938477f, MainRodPlaneX = 1.047852f, PistonAxisX = 1.075f;
        public static Vector3 Pin(Transform frame, Transform wheel, float rodPlaneX)
        {
            var pin = frame.InverseTransformPoint(wheel.TransformPoint(new Vector3(.98730177f, -CrankRadius, 0)));
            pin.x = rodPlaneX / Mathf.Max(.001f, Mathf.Abs(frame.lossyScale.x));
            return frame.TransformPoint(pin);
        }
        public static Vector3 NativeThreadFoot(string meshName, Bounds bounds) => new Vector3((meshName.StartsWith("OilCup02", System.StringComparison.Ordinal) || meshName.StartsWith("OilCup03", System.StringComparison.Ordinal)) ? .035663974f : 0, bounds.min.y, 0);
        public static Vector3 ThreadTop(Transform frame, Vector3 cupOrigin, Vector3 foot, Quaternion orientation) => cupOrigin + orientation * foot + frame.up * ThreadEmbed;
        public static Vector3 SeatedOrigin(Transform frame, Vector3 surface, Vector3 foot, Quaternion orientation) => surface - orientation * foot - frame.up * ThreadEmbed;
        public static Vector3 ThreadTop(Transform frame, Vector3 cupOrigin, float meshMinY) =>
            cupOrigin + frame.up * (meshMinY + ThreadEmbed);
        public static Vector3 SeatedOrigin(Transform frame, Vector3 surface, float meshMinY) =>
            surface - frame.up * (meshMinY + ThreadEmbed);
    }
}
