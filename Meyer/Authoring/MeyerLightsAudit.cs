using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RearrangedS060;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MeyerLightsAudit
{
    [Serializable] private class Data { public Entry[] meshes = Array.Empty<Entry>(); public int nativeStemRendererCount = 0; public NativeLodGroup stemLodGroup = null; }
    [Serializable] private class NativeLodGroup { public float size = 0; public Vector3 referencePoint = Vector3.zero; public NativeLod[] levels = Array.Empty<NativeLod>(); }
    [Serializable] private class NativeLod { public float screenRelativeHeight = 0, fadeTransitionWidth = 0; public bool containsStem = false; public int originalRendererCount = 0; }
    [Serializable] private class Entry { public string name = ""; public Vector3[] vertices = Array.Empty<Vector3>(); public int[] triangles = Array.Empty<int>(); }
    public static void Validate(string output)
    {
        string preview = Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW");
        string fixture = Path.Combine(Path.GetDirectoryName(preview), "meyer-lights-native.json");
        if (!File.Exists(fixture)) throw new InvalidOperationException("Run Prepare.MeyerLightsEvidence.py before the Meyer light audit.");
        var data = JsonUtility.FromJson<Data>(File.ReadAllText(fixture));
        var owned = new List<Object>(); var car = new GameObject("Transient native lamp QA"); owned.Add(car);
        var body = Group(car.transform, "Body");
        foreach (string lod in new[] { "LOD0", "LOD1" })
            foreach (string side in new[] { "left", "right" })
            { var part = Group(Group(body, lod + side), "s060_taillight_lower_" + side); part.localScale = new Vector3(-1, 1, 1); }
        var stem = Group(body, "s060_buffer_stems").gameObject.AddComponent<MeshFilter>();
        stem.gameObject.AddComponent<MeshRenderer>();
        var source = Mesh(data.meshes.Single(m => m.name == "s060_buffer_stems")); owned.Add(source); stem.sharedMesh = source;
        Require(data.nativeStemRendererCount == 1 && data.stemLodGroup != null && data.stemLodGroup.levels.Length == 4,
            "Actual native stem LOD membership fixture is required.");
        var stemRenderer = stem.GetComponent<Renderer>();
        var unrelated = new List<Renderer>();
        for (int i = 0; i < data.stemLodGroup.levels.Max(l => l.originalRendererCount); i++)
        {
            var go = Group(body, "Unrelated native LOD renderer fixture " + i).gameObject;
            go.AddComponent<MeshFilter>().sharedMesh = source; unrelated.Add(go.AddComponent<MeshRenderer>());
        }
        var group = body.gameObject.AddComponent<LODGroup>();
        group.SetLODs(data.stemLodGroup.levels.Select(level => {
            var renderers = unrelated.Take(level.originalRendererCount - (level.containsStem ? 1 : 0)).ToList();
            if (level.containsStem) renderers.Add(stemRenderer);
            return new LOD(level.screenRelativeHeight, renderers.ToArray()) { fadeTransitionWidth = level.fadeTransitionWidth };
        }).ToArray());
        group.localReferencePoint = data.stemLodGroup.referencePoint; group.size = data.stemLodGroup.size;
        var originalLods = group.GetLODs(); var originalLodPoint = group.localReferencePoint; float originalLodSize = group.size;
        var rear = Group(Group(car.transform, "LocoS060_Headlights"), "RearSide");
        var glass = Group(rear, "s060_headlight_glass_R_LO").gameObject.AddComponent<MeshFilter>();
        var glassMesh = Mesh(data.meshes.Single(m => m.name == "s060_headlight_glass_R_LO")); owned.Add(glassMesh); glass.sharedMesh = glassMesh;
        var left = Group(rear, "HeadlightsLowLeft").gameObject.AddComponent<Light>(); left.intensity = 2;
        var right = Group(rear, "HeadlightsLowRight").gameObject.AddComponent<Light>(); right.gameObject.SetActive(false);
        var upper = Group(rear, "HeadlightHigh"); upper.localPosition = new Vector3(0, 3.1f, -4.3f);
        var buffers = Group(car.transform, "[buffers]");
        var plate = Group(buffers, "HookPlate_R");
        var bufferRig = Group(buffers, "BuffersAndChainRig"); bufferRig.localPosition = new Vector3(0, 1.05f, -8.0517f); bufferRig.localRotation = Quaternion.Euler(0, 180, 0);
        var anchorLeft = Group(bufferRig, "buffer anchor left"); anchorLeft.localPosition = new Vector3(-.864f, 0, .3f);
        var anchorRight = Group(bufferRig, "buffer anchor right"); anchorRight.localPosition = new Vector3(.864f, 0, .3f);
        var headLeft = Group(anchorLeft, "Buffer_RR"); headLeft.localPosition = new Vector3(.0109f, -.0356f, -.07175f);
        var headRight = Group(anchorRight, "Buffer_RL"); headRight.localPosition = new Vector3(-.0109f, -.0356f, -.07175f);
        var headMesh = Mesh(data.meshes.Single(m => m.name == "Buffer04")); owned.Add(headMesh);
        headLeft.gameObject.AddComponent<MeshFilter>().sharedMesh = headMesh; headRight.gameObject.AddComponent<MeshFilter>().sharedMesh = headMesh;
        var frame = Group(car.transform, "RearFrame"); frame.localPosition = new Vector3(0, 0, MeyerPlan.RearCentre);
        var original = car.GetComponentsInChildren<Transform>(true).Where(t => t != frame).Select(t => new { t, parent = t.parent, pos = t.localPosition, rot = t.localRotation, scale = t.localScale, sibling = t.GetSiblingIndex(), active = t.gameObject.activeSelf }).ToArray();
        int checks = 0;
        try
        {
            foreach (float bodyYaw in new[] { 0f, 73f }) foreach (float yaw in new[] { -15f, 0f, 15f })
            {
                car.transform.SetPositionAndRotation(new Vector3(120, 15, -83), Quaternion.Euler(0, bodyYaw, 0));
                frame.localRotation = Quaternion.Euler(0, yaw, 0);
                var topPosition = upper.position;
                using (var binding = S060MeyerLights.Bind(car.transform, body, frame, new Vector3(0, 0, -3.70f)))
                {
                    foreach (int index in glassMesh.triangles.Distinct())
                    {
                        var v = glassMesh.vertices[index]; var expected = frame.TransformPoint(v + new Vector3(0, 0, MeyerPlan.RearLampGlassZ - MeyerPlan.NativeRearLampGlassZ));
                        Require(Vector3.Distance(glass.transform.TransformPoint(v), expected) < .0001f, "Native rear glass must follow the rear-frame datum."); checks++;
                    }
                    var before = source.vertices; var after = stem.sharedMesh.vertices;
                    for (int i = 0; i < before.Length; i++)
                    { Require(after[i] == before[i], "Original front stem geometry must stay exact."); checks++; }
                    Require(stem.sharedMesh.triangles.All(i => before[i].z >= 0), "Original stem renderer must retain only front triangles."); checks++;
                    var rearStem = frame.Find("[Meyer native rear buffer stems]").GetComponent<MeshFilter>();
                    var activeLods = group.GetLODs(); var rearRenderer = rearStem.GetComponent<Renderer>();
                    for (int level = 0; level < activeLods.Length; level++)
                    {
                        bool belongs = originalLods[level].renderers.Contains(stemRenderer);
                        Require(activeLods[level].screenRelativeTransitionHeight == originalLods[level].screenRelativeTransitionHeight &&
                            activeLods[level].fadeTransitionWidth == originalLods[level].fadeTransitionWidth &&
                            activeLods[level].renderers.Count(r => r == rearRenderer) == (belongs ? 1 : 0) &&
                            activeLods[level].renderers.Where(r => r != rearRenderer).SequenceEqual(originalLods[level].renderers),
                            "Rear stems must join only the source's native LOD levels and preserve every existing renderer/threshold."); checks++;
                    }
                    var used = rearStem.sharedMesh.triangles.Distinct().ToArray();
                    float nativeFlange = before.Where(p => p.z < 0).Max(p => p.z);
                    float deltaZ = MeyerPlan.RearBeamCentreZ + MeyerPlan.RearBeamDepth / 2 - nativeFlange;
                    foreach (int index in used)
                    {
                        var expected = before[index] + new Vector3(0, 0, deltaZ);
                        Require(Vector3.Distance(rearStem.transform.TransformPoint(rearStem.sharedMesh.vertices[index]), frame.TransformPoint(expected)) < .0001f, "Rear native stem geometry must follow the moving beam."); checks++;
                    }
                    Require(Mathf.Abs(used.Max(i => rearStem.sharedMesh.vertices[i].z) - (MeyerPlan.RearBeamCentreZ + MeyerPlan.RearBeamDepth / 2)) < .00001f, "Native stem mounting flange must meet beam front face."); checks++;
                    var wrapper = frame.Find("[Meyer native rear buffers]");
                    Require(anchorLeft.parent == bufferRig && anchorRight.parent == bufferRig && bufferRig.parent == wrapper &&
                        bufferRig.localPosition.sqrMagnitude < .000001f && Quaternion.Angle(bufferRig.localRotation, Quaternion.identity) < .001f &&
                        anchorLeft.localPosition == new Vector3(-.864f, 0, .3f), "Native controller rig and anchors retain one compression datum."); checks++;
                    foreach (float compressionZ in new[] { .3f, .15f, 0 })
                    {
                        anchorLeft.localPosition = new Vector3(-.864f, 0, compressionZ);
                        var expected = frame.TransformPoint(wrapper.localPosition + wrapper.localRotation * (anchorLeft.localPosition + headLeft.localPosition));
                        Require(Vector3.Distance(headLeft.position, expected) < .0001f, "Native buffer head must track wrapper yaw and compression."); checks++;
                        var headPoints = headMesh.vertices.Select(p => frame.InverseTransformPoint(headLeft.TransformPoint(p))).ToArray();
                        var stemPoints = used.Select(i => rearStem.sharedMesh.vertices[i]).Where(p => p.x > 0).ToArray();
                        Require(Bounds(headPoints).Intersects(Bounds(stemPoints)), "Native rear head and stem must remain connected throughout buffer compression and yaw."); checks++;
                        Require(Bounds(stemPoints).Intersects(new Bounds(new Vector3(0, 1.05f, MeyerPlan.RearBeamCentreZ), new Vector3(2.35f, .25f, MeyerPlan.RearBeamDepth))), "Native rear stem must meet the authored moving beam."); checks++;
                    }
                    anchorLeft.localPosition = new Vector3(-.864f, 0, .3f);
                    Require(left.transform.parent == frame && right.transform.parent == frame && left.intensity == 2 && !right.gameObject.activeSelf, "Lamp instances and native control state must survive relocation."); checks++;
                    Require(upper.position == topPosition && upper.parent == rear, "Upper rear lamp must stay on the S060."); checks++;
                }
                Require(stem.sharedMesh == source, "Native shared stem mesh must restore."); checks++;
                var restoredLods = group.GetLODs();
                for (int level = 0; level < restoredLods.Length; level++)
                { Require(restoredLods[level].renderers.SequenceEqual(originalLods[level].renderers), "Native LOD renderer arrays must restore exactly."); checks++; }
                Require(group.localReferencePoint == originalLodPoint && group.size == originalLodSize, "Original native LOD bounds must restore."); checks++;
                foreach (var home in original.Where(p => p.t != car.transform))
                { Require(home.t.parent == home.parent && home.t.localPosition == home.pos && home.t.localRotation == home.rot && home.t.localScale == home.scale && home.t.GetSiblingIndex() == home.sibling && home.t.gameObject.activeSelf == home.active,
                    "Every moved native transform must restore exactly: " + home.t.name + "; sibling " + home.t.GetSiblingIndex() + "/" + home.sibling + "; position " + home.t.localPosition + "/" + home.pos); checks++; }
            }
            File.WriteAllText(Path.Combine(output, "meyer-lights-validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"nativeStemGlassAndBuffer04Geometry\":true,\"nativeStemRendererCount\":1,\"sourceStemLodLevels\":[0,1],\"rearStemMatchesNativeLods\":true,\"originalLodArraysAndBoundsRestored\":true,\"yawDegrees\":[-15,0,15],\"bufferCompressionCoordinates\":[0.3,0.15,0],\"headStemBeamBoundsConnected\":true,\"upperLampUnmoved\":true,\"gameRuntimeValidated\":false}");
        }
        finally { foreach (var item in owned) if (item != null) Object.DestroyImmediate(item); }
    }
    private static Transform Group(Transform parent, string name) { var part = new GameObject(name).transform; part.SetParent(parent, false); return part; }
    private static Mesh Mesh(Entry entry) { var mesh = new Mesh { name = entry.name + " local QA" }; mesh.vertices = entry.vertices; mesh.triangles = entry.triangles; mesh.RecalculateBounds(); return mesh; }
    private static Bounds Bounds(Vector3[] points) { var bounds = new Bounds(points[0], Vector3.zero); foreach (var point in points) bounds.Encapsulate(point); return bounds; }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
