using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CCL.Types.Components;
using RearrangedS060;
using UnityEngine;

// Actual local B99 wheel triangles establish pin length/radius and eccentric
// shoulder clearance. No inference from a wheel's overall AABB is accepted.
public static class SteamNativePinAudit
{
    [Serializable] private class Data { public Entry[] meshes = Array.Empty<Entry>(); }
    [Serializable] private class Entry { public string name = ""; public Vector3[] vertices = Array.Empty<Vector3>(); public int[] triangles = Array.Empty<int>(); }
    public static float PinRadius { get; private set; }
    public static void Validate(GameObject root, string output, bool dockside)
    {
        var path = Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW");
        var data = JsonUtility.FromJson<Data>(File.ReadAllText(path));
        var aliases = root.GetComponentsInChildren<MeshGrabberFilter>(true).Where(a => a.name.Contains("driver ")).ToArray();
        Require(aliases.Length == 8, "Eight native wheel-side aliases required.");
        int checks = 0; float minimumBoreGap = float.PositiveInfinity, minimumShoulderGap = float.PositiveInfinity; PinRadius = 0;
        foreach (var alias in aliases)
        {
            bool rear = alias.transform.parent.name.Contains("rear");
            string required = rear ? "s060_Wheels_01" : "s060_Wheels_02";
            Require(alias.ReplacementName == required, "Each main-rod axle needs the real long-pin native01 wheel; front axle uses native02."); checks++;
            var native = data.meshes.Single(e => e.name == required);
            float radial = Mathf.Abs(alias.transform.localScale.y);
            float hole = dockside ? .042f : .0595f;
            var planes = rear ? new[] { S060OilCupGeometry.CouplingRodPlaneX, S060OilCupGeometry.MainRodPlaneX } : new[] { S060OilCupGeometry.CouplingRodPlaneX };
            foreach (float plane in planes) foreach (float face in new[] { plane - .024f, plane + .024f })
            {
                var ring = new List<float>();
                for (int tri = 0; tri < native.triangles.Length; tri += 3)
                    for (int edge = 0; edge < 3; edge++)
                    {
                        var a = native.vertices[native.triangles[tri + edge]]; var b = native.vertices[native.triangles[tri + (edge + 1) % 3]];
                        if ((a.x < face) == (b.x < face) || Mathf.Abs(b.x - a.x) < .000001f) continue;
                        var p = Vector3.LerpUnclamped(a, b, (face - a.x) / (b.x - a.x));
                        float r = new Vector2(p.y + S060OilCupGeometry.CrankRadius, p.z).magnitude;
                        if (r < .10f) ring.Add(r);
                    }
                Require(ring.Count >= 12, "Actual native pin does not span both faces of this rod bearing."); checks++;
                float actualRadius = ring.Max(); PinRadius = Mathf.Max(PinRadius, actualRadius);
                float gap = hole - actualRadius * radial; minimumBoreGap = Mathf.Min(minimumBoreGap, gap);
                Require(gap >= .001f, "Native pin shaft clips the actual bored rod eye."); checks++;
            }
            if (rear)
            {
                var shoulder = native.vertices.Where(v => v.x > 1f && new Vector2(v.y + S060OilCupGeometry.CrankRadius, v.z).magnitude > .12f && new Vector2(v.y + S060OilCupGeometry.CrankRadius, v.z).magnitude < .20f).ToArray();
                Require(shoulder.Length >= 12, "Native01 eccentric shoulder was not measured."); checks++;
                float gap = shoulder.Min(v => v.x) - (S060OilCupGeometry.MainRodPlaneX + .024f); minimumShoulderGap = Mathf.Min(minimumShoulderGap, gap);
                Require(gap >= .008f, "Main rod intersects the actual native eccentric shoulder in X."); checks++;
            }
        }
        foreach (var crosshead in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Crosshead"))
        {
            float needed = S060OilCupGeometry.PistonAxisX - S060OilCupGeometry.MainRodPlaneX + .0175f;
            Require(crosshead.localScale.x / 2 - needed >= .005f, "Native piston axis is outside its supported crosshead block."); checks++;
        }
        File.WriteAllText(Path.Combine(output, "native-pin-fit-validation.json"), "{\"passed\":true,\"actualNativeTriangles\":true,\"checks\":" + checks + ",\"rearMesh\":\"s060_Wheels_01\",\"frontMesh\":\"s060_Wheels_02\",\"couplingPlaneXMetres\":0.938477,\"mainPlaneXMetres\":1.047852,\"pistonAxisXMetres\":1.075,\"maximumNativePinRadiusMetres\":" + PinRadius.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"minimumBoreGapMetres\":" + minimumBoreGap.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"minimumEccentricShoulderGapMetres\":" + minimumShoulderGap.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"nativeMeshesPackaged\":false}");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
