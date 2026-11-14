#if B99_S060_ARTICULATED || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // Move the existing lamps, glass and native controlled light/glare groups.
    // Their controller references and materials remain the original instances.
    internal sealed class S060MeyerLights : IDisposable
    {
        private sealed class Home
        {
            internal Transform Part, Parent;
            internal Vector3 Position, Scale;
            internal Quaternion Rotation;
            internal int Sibling;
        }
        private readonly List<Home> homes = new List<Home>();
        private readonly List<(MeshFilter filter, Mesh source, Mesh copy)> meshes = new List<(MeshFilter, Mesh, Mesh)>();
        private readonly List<GameObject> added = new List<GameObject>();
        private readonly List<(LODGroup group, LOD[] lods, Vector3 point, float size)> lods = new List<(LODGroup, LOD[], Vector3, float)>();
        // Meyer R2: lamps go to the new body-mounted rear bulkhead (face z -8.11), the
        // upper cab-back lamp too (the coal hopper hides it). Frame-local = car - (0,0,RearCentre).
        // Housings are ~0.13 m deep behind the glass: stand them fully proud of the bulkhead face.
        internal const float BodyRearGlassZ = MeyerPlan.BulkheadRear - .13f - MeyerPlan.RearCentre;
        internal const float NativeUpperGlassZ = -4.30f;
        // The upper housing is open at the back: it drops until its top is under the bulkhead's
        // beaded top, so the open back is never seen from above (James, 2026-10-28).
        internal const float UpperHousingTopY = MeyerPlan.BulkheadTop - .03f;
        internal static S060MeyerLights Bind(Transform car, Transform body, Transform rearFrame, Vector3 rearOffset, bool bodyMounted = false)
        {
            var owner = new S060MeyerLights();
            try
            {
                var meshes = body.GetComponentsInChildren<Transform>(true).Where(t =>
                    t.name == "s060_taillight_lower_left" || t.name == "s060_taillight_lower_right").ToArray();
                if (!meshes.Any(t => t.name.EndsWith("left", StringComparison.Ordinal)) || !meshes.Any(t => t.name.EndsWith("right", StringComparison.Ordinal)))
                    throw new InvalidOperationException("Native split lower rear lamp housings are missing.");
                var rear = car.Find("LocoS060_Headlights/RearSide");
                if (rear == null) throw new InvalidOperationException("Native rear headlight controller group is missing.");
                var parts = meshes.Concat(new[] { rear.Find("s060_headlight_glass_R_LO"), rear.Find("HeadlightsLowLeft"), rear.Find("HeadlightsLowRight") }).ToArray();
                if (parts.Any(t => t == null)) throw new InvalidOperationException("Native lower rear glass or controlled lamp effects are missing.");
                var upper = new Transform[0];
                float upperDrop = 0;
                if (bodyMounted)
                {
                    upper = body.GetComponentsInChildren<Transform>(true).Where(t => t.name == "s060_taillight_upper")
                        .Concat(rear.Cast<Transform>().Where(t => t.name == "HeadlightHigh" ||
                            (t.name.StartsWith("s060_headlight_glass", StringComparison.Ordinal) && t.name != "s060_headlight_glass_R_LO"))).ToArray();
                    if (upper.Length < 3) throw new InvalidOperationException("Native upper rear lamp housing, glass or controlled effect is missing.");
                    parts = parts.Concat(upper).ToArray();
                    upperDrop = Mathf.Max(0, HousingTop(car, upper.Where(t => t.name == "s060_taillight_upper")) - UpperHousingTopY);
                }
                foreach (var part in parts)
                    owner.homes.Add(new Home { Part = part, Parent = part.parent, Position = part.localPosition,
                        Rotation = part.localRotation, Scale = part.localScale, Sibling = part.GetSiblingIndex() });
                foreach (var part in parts)
                {
                    var local = car.InverseTransformPoint(part.position);
                    var rotation = Quaternion.Inverse(car.rotation) * part.rotation;
                    var matrix = car.worldToLocalMatrix * part.localToWorldMatrix;
                    part.SetParent(rearFrame, false);
                    // Native mesh vertices and effect children are authored in
                    // car coordinates. The new rear datum is in frame space.
                    float target = bodyMounted ? BodyRearGlassZ : MeyerPlan.RearLampGlassZ;
                    float native = upper.Contains(part) ? NativeUpperGlassZ : MeyerPlan.NativeRearLampGlassZ;
                    part.localPosition = local + new Vector3(0, upper.Contains(part) ? -upperDrop : 0, target - native);
                    part.localRotation = rotation; part.localScale = matrix.lossyScale;
                }
                var plate = car.Find("[buffers]/HookPlate_R");
                if (plate == null) throw new InvalidOperationException("Native rear hook plate is missing.");
                owner.homes.Add(new Home { Part = plate, Parent = plate.parent, Position = plate.localPosition,
                    Rotation = plate.localRotation, Scale = plate.localScale, Sibling = plate.GetSiblingIndex() });
                owner.BindBuffers(car, rearFrame);
                var platePosition = car.InverseTransformPoint(plate.position);
                var plateRotation = Quaternion.Inverse(car.rotation) * plate.rotation;
                plate.SetParent(rearFrame, false);
                plate.localPosition = platePosition + new Vector3(0, 0, 2.105f);
                plate.localRotation = plateRotation;
                var stems = body.GetComponentsInChildren<MeshFilter>(true).Where(f => f.name == "s060_buffer_stems").ToArray();
                if (stems.Length == 0) throw new InvalidOperationException("Native buffer stem mesh is missing.");
                foreach (var filter in stems)
                {
                    var source = filter.sharedMesh;
                    if (source == null || !source.isReadable) throw new InvalidOperationException("Native S060 buffer stems must be CPU-readable.");
                    var copy = UnityEngine.Object.Instantiate(source); copy.name = source.name + " (Meyer rear stems)";
                    owner.meshes.Add((filter, source, copy));
                    var vertices = source.vertices;
                    var rearMask = new bool[vertices.Length];
                    var rearVertices = new Vector3[vertices.Length];
                    float flangeZ = float.NegativeInfinity;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var local = car.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                        rearMask[i] = local.z < 0;
                        rearVertices[i] = rearMask[i] ? local + rearOffset - new Vector3(0, 0, MeyerPlan.RearCentre) : Vector3.zero;
                        if (rearMask[i]) flangeZ = Mathf.Max(flangeZ, rearVertices[i].z);
                    }
                    // R2 keeps the beam at the old rear beam, so the stem flanges stay on its face.
                    float flangeTarget = MeyerPlan.RearBeamCentreZ + MeyerPlan.RearBeamDepth / 2;
                    float flangeCorrection = flangeTarget - flangeZ;
                    for (int i = 0; i < rearVertices.Length; i++) if (rearMask[i]) rearVertices[i].z += flangeCorrection;
                    var rearCopy = UnityEngine.Object.Instantiate(source); rearCopy.name = source.name + " (Meyer articulated rear only)";
                    var rearObject = new GameObject("[Meyer native rear buffer stems]"); owner.added.Add(rearObject); rearObject.layer = filter.gameObject.layer; rearObject.transform.SetParent(rearFrame, false);
                    var rearFilter = rearObject.AddComponent<MeshFilter>(); rearFilter.sharedMesh = rearCopy;
                    var sourceRenderer = filter.GetComponent<MeshRenderer>();
                    var rearRenderer = rearObject.AddComponent<MeshRenderer>(); rearRenderer.sharedMaterials = sourceRenderer.sharedMaterials; rearRenderer.enabled = sourceRenderer.enabled;
                    owner.meshes.Add((rearFilter, null, rearCopy));
                    for (int sub = 0; sub < source.subMeshCount; sub++)
                    {
                        var frontTriangles = new List<int>(); var rearTriangles = new List<int>(); var triangles = source.GetTriangles(sub);
                        for (int i = 0; i < triangles.Length; i += 3)
                        {
                            var target = rearMask[triangles[i]] && rearMask[triangles[i + 1]] && rearMask[triangles[i + 2]] ? rearTriangles : frontTriangles;
                            target.Add(triangles[i]); target.Add(triangles[i + 1]); target.Add(triangles[i + 2]);
                        }
                        copy.SetTriangles(frontTriangles, sub); rearCopy.SetTriangles(rearTriangles, sub);
                    }
                    rearCopy.vertices = rearVertices; rearCopy.RecalculateBounds();
                    copy.RecalculateBounds(); filter.sharedMesh = copy;
                    owner.JoinSourceLods(car, sourceRenderer, rearRenderer);
                }
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        private static float HousingTop(Transform car, IEnumerable<Transform> housings)
        {
            float top = float.NegativeInfinity;
            foreach (var filter in housings.SelectMany(h => h.GetComponentsInChildren<MeshFilter>(true)).Where(f => f.sharedMesh != null))
            {
                var b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    top = Mathf.Max(top, car.InverseTransformPoint(filter.transform.TransformPoint(corner)).y);
                }
            }
            if (float.IsNegativeInfinity(top)) throw new InvalidOperationException("Native upper rear lamp housing mesh is missing.");
            return top;
        }
        private void BindBuffers(Transform car, Transform rearFrame)
        {
            var buffers = car.Find("[buffers]");
            var rig = buffers == null ? null : buffers.Cast<Transform>().SingleOrDefault(t => t.name == "BuffersAndChainRig" && car.InverseTransformPoint(t.position).z < 0);
            if (rig == null) throw new InvalidOperationException("Native relocated rear buffer rig is missing.");
            var anchors = new[] { rig.Find("buffer anchor left"), rig.Find("buffer anchor right") };
            if (anchors.Any(t => t == null)) throw new InvalidOperationException("Native rear buffer anchors are missing.");
            var wrapper = new GameObject("[Meyer native rear buffers]"); added.Add(wrapper); wrapper.transform.SetParent(rearFrame, false);
            // Native compression measures in the controller's transform and
            // writes anchor-local Z. Move the complete rig, never its anchors
            // alone, so both operations keep the same reference frame.
            wrapper.transform.localPosition = car.InverseTransformPoint(rig.position) - new Vector3(0, 0, MeyerPlan.RearCentre);
            wrapper.transform.localRotation = Quaternion.Inverse(car.rotation) * rig.rotation;
            homes.Add(new Home { Part = rig, Parent = rig.parent, Position = rig.localPosition,
                Rotation = rig.localRotation, Scale = rig.localScale, Sibling = rig.GetSiblingIndex() });
            rig.SetParent(wrapper.transform, false);
            rig.localPosition = Vector3.zero; rig.localRotation = Quaternion.identity;
        }
        private void JoinSourceLods(Transform car, Renderer source, Renderer addedRenderer)
        {
            foreach (var group in car.GetComponentsInChildren<LODGroup>(true))
            {
                var current = group.GetLODs();
                if (!current.Any(lod => lod.renderers.Contains(source))) continue;
                if (!lods.Any(home => home.group == group)) lods.Add((group, current, group.localReferencePoint, group.size));
                var updated = current.ToArray();
                for (int i = 0; i < updated.Length; i++)
                    if (updated[i].renderers.Contains(source))
                        updated[i].renderers = updated[i].renderers.Concat(new[] { addedRenderer }).ToArray();
                group.SetLODs(updated); group.RecalculateBounds();
            }
        }
        internal void ReleaseMeshes()
        {
            foreach (var mesh in meshes)
            {
                if (mesh.filter != null) mesh.filter.sharedMesh = mesh.source;
                if (mesh.copy != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(mesh.copy); else UnityEngine.Object.DestroyImmediate(mesh.copy); }
            }
            meshes.Clear();
        }
        public void Dispose()
        {
            foreach (var home in lods)
                if (home.group != null) { home.group.SetLODs(home.lods); home.group.localReferencePoint = home.point; home.group.size = home.size; }
            lods.Clear();
            ReleaseMeshes();
            foreach (var home in homes)
            {
                if (home.Part == null) continue;
                home.Part.SetParent(home.Parent, false);
                home.Part.localPosition = home.Position; home.Part.localRotation = home.Rotation; home.Part.localScale = home.Scale;
            }
            // Restore every parent before rebuilding its original child order.
            foreach (var home in homes.OrderBy(home => home.Sibling))
                if (home.Part != null) home.Part.SetSiblingIndex(home.Sibling);
            homes.Clear();
            foreach (var part in added) if (part != null) { part.SetActive(false); if (Application.isPlaying) UnityEngine.Object.Destroy(part); else UnityEngine.Object.DestroyImmediate(part); }
            added.Clear();
        }
    }
}
#endif
