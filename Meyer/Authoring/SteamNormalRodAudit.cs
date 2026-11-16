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
    [Serializable] private class Data { public Entry[] meshes; }
    [Serializable] private class Entry { public string name; public Vector3[] vertices; public Vector2[] uv; public int[] triangles; }
    public static void Validate(string output)
    {
        int checks = 0; float maximum = 0;
        var source=JsonUtility.FromJson<Data>(File.ReadAllText(Environment.GetEnvironmentVariable("S060_CONVENTIONAL_NATIVE"))).meshes.Single(m=>m.name=="s060_mech_wheels_connect");
        foreach (int count in new[] { 2, 4, 5 }) foreach (int side in new[] { -1, 1 }) foreach (float scale in new[] { .8f, 1f })
        {
            var frame = new GameObject("QA conventional rod frame").transform; frame.localScale = new Vector3(side, scale, scale); frame.rotation = Quaternion.Euler(0, 23, 0);
            var rod = new GameObject("QA native animated rod").transform; rod.SetParent(frame, false); rod.localRotation = Quaternion.Euler(-.000694f, 0, 0);
            var filter = rod.gameObject.AddComponent<MeshFilter>(); var original = new Mesh { name=source.name,vertices=source.vertices,uv=source.uv,triangles=source.triangles }; original.RecalculateNormals();original.RecalculateBounds();filter.sharedMesh = original;
            var positions = count == 2 ? new[] { -1.399902f, 1.699951f } : Enumerable.Range(0,count).Select(i=>1.699951f-(count-1-i)*1.5493f).ToArray();
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
                Require(mesh.name.Contains("native") && mesh.uv.All(uv=>original.uv.Contains(uv)), "Fitted rod must retain native geometry and atlas islands."); checks++;
                var centres = pins.Select(pin=>
                {
                    var cap=vertices.Where(v=>new Vector2(v.y-pin.x,v.z-pin.y).sqrMagnitude<.000001f).ToArray();
                    Require(cap.Length>=2,"Every native eye retains both cap centres");checks++;
                    return cap.Aggregate(Vector3.zero,(sum,v)=>sum+v)/cap.Length;
                }).ToArray();
                for (int sample = 0; sample <= 128; sample++)
                {
                    Pose(sample / 128f);
                    for (int i = 0; i < count; i++)
                    {
                        float gap = Vector3.Distance(rod.TransformPoint(centres[i]), S060OilCupGeometry.Pin(frame, wheels[i], .938477f)); maximum = Mathf.Max(maximum, gap);
                        Require(gap < .00015f, "Native coupling-eye centre detached beyond retained source precision."); checks++;
                    }
                }
                swap.Dispose(); swap = null;
                Require(ReferenceEquals(filter.sharedMesh, original) && original.vertexCount == source.vertices.Length, "Restoring stock must restore the exact untouched source mesh."); checks++;
            }
            finally { swap?.Dispose(); Object.DestroyImmediate(frame.gameObject); Object.DestroyImmediate(original); }
        }
        File.WriteAllText(Path.Combine(output, "normal-rod-validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"pinCounts\":[2,4,5],\"scaleFactors\":[0.8,1],\"samplesPerSideAndScale\":129,\"maximumBossGapMetres\":" + maximum.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"originalMeshRestored\":true,\"actualNativeAnimationRuntimeTested\":false}");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
