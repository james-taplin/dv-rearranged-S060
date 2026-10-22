using System;
using System.IO;
using System.Linq;
using RearrangedS060;
using UnityEngine;
using Object = UnityEngine.Object;

// Native wheel/rod datums from the B99 probe; actual production mesh builder
// and reversible mesh swap, sampled throughout rotation at both fixed scales.
public static class SteamNormalRodAudit
{
    public static void Validate(string output)
    {
        int checks = 0; float maximum = 0;
        foreach (int count in new[] { 2, 4 }) foreach (int side in new[] { -1, 1 }) foreach (float scale in new[] { .8f, 1f })
        {
            var frame = new GameObject("QA conventional rod frame").transform; frame.localScale = new Vector3(side, scale, scale); frame.rotation = Quaternion.Euler(0, 23, 0);
            var rod = new GameObject("QA native animated rod").transform; rod.SetParent(frame, false); rod.localRotation = Quaternion.Euler(-.000694f, 0, 0);
            var filter = rod.gameObject.AddComponent<MeshFilter>(); var original = new Mesh { name = "QA original native mesh sentinel" }; original.vertices = new[] { Vector3.zero, Vector3.up, Vector3.forward }; original.triangles = new[] { 0, 1, 2 }; filter.sharedMesh = original;
            var positions = count == 2 ? new[] { -1.399902f, 1.699951f } : new[] { -2.949202f, -1.399902f, .149902f, 1.699951f };
            var wheels = positions.Select(z => { var w = new GameObject("QA native driver").transform; w.SetParent(frame, false); w.localPosition = new Vector3(0, .712128f, z); return w; }).ToArray();
            S060RodMeshSwap swap = null;
            try
            {
                void Pose(float turns)
                {
                    foreach (var wheel in wheels) wheel.localRotation = Quaternion.Euler(turns * 360 + (side > 0 ? -90 : 0), 0, 0);
                    var p = frame.InverseTransformPoint(S060OilCupGeometry.Pin(frame, wheels[count == 2 ? 0 : 1], .938477f));
                    rod.localPosition = p + new Vector3(0, -.002538f, -.00004f);
                }
                Pose(0);
                var pins = wheels.Select(w => rod.InverseTransformPoint(S060OilCupGeometry.Pin(frame, w, .938477f))).Select(p => new Vector2(p.y, p.z)).ToArray();
                swap = new S060RodMeshSwap(filter, pins);
                var mesh = filter.sharedMesh; var vertices = mesh.vertices;
                float bore = vertices.Take(512).Min(v => new Vector2(v.y - pins[0].x, v.z - pins[0].y).magnitude);
                Require((bore - SteamNativePinAudit.PinRadius) * scale >= .001f, "Conventional rod bore clips the native pin shaft."); checks++;
                Require(vertices.Length >= count * 512 && !ReferenceEquals(mesh, original), "Fitted rod must have one bored boss per live wheel."); checks++;
                // Measure the actual generated boss surface centres, rather than
                // accepting an attachment marker independent of visible geometry.
                var centres = Enumerable.Range(0, count).Select(i => vertices.Skip(i * 512).Take(512).Aggregate(Vector3.zero, (sum, v) => sum + v) / 512).ToArray();
                for (int sample = 0; sample <= 128; sample++)
                {
                    Pose(sample / 128f);
                    for (int i = 0; i < count; i++)
                    {
                        float gap = Vector3.Distance(rod.TransformPoint(centres[i]), S060OilCupGeometry.Pin(frame, wheels[i], .938477f)); maximum = Mathf.Max(maximum, gap);
                        Require(gap < .0001f, "Visible coupling-rod boss detached from the native wheel pin."); checks++;
                    }
                }
                swap.Dispose(); swap = null;
                Require(ReferenceEquals(filter.sharedMesh, original) && original.vertexCount == 3, "Restoring stock must restore the exact untouched source mesh."); checks++;
            }
            finally { swap?.Dispose(); Object.DestroyImmediate(frame.gameObject); Object.DestroyImmediate(original); }
        }
        File.WriteAllText(Path.Combine(output, "normal-rod-validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"pinCounts\":[2,4],\"scaleFactors\":[0.8,1],\"samplesPerSideAndScale\":129,\"maximumBossGapMetres\":" + maximum.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"originalMeshRestored\":true,\"actualNativeAnimationRuntimeTested\":false}");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
