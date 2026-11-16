using UnityEngine;

namespace RearrangedS060
{
    // Shared by live mounting and reopened-bundle sweep audits. Native cup
    // origins are above the threaded stem, not at the bottom of the mesh.
    public static class S060OilCupGeometry
    {
        public static Vector3 GuideSeat(Transform body, Vector3 reference)
        {
            float nearest = .04f; var seat = Vector3.zero; bool found = false;
            foreach (var filter in body.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.name != "s060_body" || filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
                var mesh = filter.sharedMesh; var vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++) vertices[i] = body.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
                    float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                    if (Mathf.Abs(denominator) < .0000001f) continue;
                    float u = ((b.z - c.z) * (reference.x - c.x) + (c.x - b.x) * (reference.z - c.z)) / denominator;
                    float v = ((c.z - a.z) * (reference.x - c.x) + (a.x - c.x) * (reference.z - c.z)) / denominator;
                    if (u < 0 || v < 0 || u + v > 1) continue;
                    float y = u * a.y + v * b.y + (1 - u - v) * c.y;
                    float distance = Mathf.Abs(y - reference.y);
                    if (distance >= nearest) continue;
                    nearest = distance; seat = new Vector3(reference.x, y, reference.z); found = true;
                }
            }
            if (!found) throw new System.InvalidOperationException("Native S060 upper slidebar mounting face missing.");
            return seat;
        }
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
