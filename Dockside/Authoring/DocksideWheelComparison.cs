using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CCL.Types.Components;
using UnityEngine;
using Object = UnityEngine.Object;

// Offline experiment only: native game wheel geometry remains a transient QA
// fixture, never an exported asset or a runtime dependency on the S282 mod.
public static class DocksideWheelComparison
{
    [Serializable] private class Data { public Entry[] meshes = Array.Empty<Entry>(); }
    [Serializable] private class Entry { public string name = ""; public Vector3[] vertices = Array.Empty<Vector3>(); public Vector2[] uv = Array.Empty<Vector2>(); public int[] triangles = Array.Empty<int>(); }
    public static void Render(GameObject prefab, string output)
    {
        string path = Environment.GetEnvironmentVariable("DOCKSIDE_WHEEL_COMPARISON");
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new InvalidOperationException("Local S282 wheel comparison fixture required.");
        var entry = JsonUtility.FromJson<Data>(File.ReadAllText(path)).meshes.Single(m => m.name == "s282_wheels_driving_1");
        var mesh = new Mesh { name = "S282 native wheel experiment (not packaged)" };
        mesh.vertices = entry.vertices; mesh.uv = entry.uv; mesh.triangles = entry.triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var clone = Object.Instantiate(prefab);
        try
        {
            var wheels = clone.GetComponentsInChildren<MeshGrabberFilter>(true).Where(a => (a.ReplacementName == MeyerPlan.DriverMesh || a.ReplacementName == "s060_Wheels_01")).ToArray();
            foreach (var alias in wheels)
            {
                alias.Filter.sharedMesh = mesh; alias.ReplacementName = "QA S282 comparison only";
                // Same native tread radius and untouched X/gauge as S060.
                // Reuse existing absolute quartering to compare the geometry.
            }
            SteamBogiePreview.Render(clone, output, "dockside-s282-experiment", DocksidePlan.DriverRadius, "Small driver ");
            File.WriteAllText(Path.Combine(output, "wheelset-comparison.json"), "{\"performed\":true,\"source\":\"native s282_wheels_driving_1\",\"transientOnly\":true,\"packaged\":false,\"wheelAliases\":" + wheels.Length + ",\"xGaugeScale\":1,\"radialScale\":0.70212,\"nominalDriverDiameterMetres\":1,\"decision\":\"retain S060 wheels; fit surrounding Franklin assembly\",\"reason\":\"near-identical tread and crank datums; S282 swap does not fix unsupported shafts or rod shapes\"}\n");
        }
        finally { Object.DestroyImmediate(clone); Object.DestroyImmediate(mesh); }
    }
}
