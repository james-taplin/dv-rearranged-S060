using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RearrangedS060;
using UnityEngine;
using Object = UnityEngine.Object;

// Local native meshes are transient QA fixtures. Only the owned return pipe
// is built by production code; no native art is written into the asset pack.
public static class MeyerExhaustPipeAudit
{
    [Serializable] public sealed class Report
    {
        public bool passed, actualValveChestAndSmokeboxTriangles, actualDrawnCapsAndSleeves,
            actualRunningGearTriangleIntersections, independentBodyAndBogiePoses,
            movingSleevesClearNativeBodyAndDeck, movingSleevesClearSupplyPipes, allExhaustPartsClearSupplyPipes, fixedOutboardRunClearNativeEnvelope,
            supportsMeetActualNativePlate,
            stockSourcesRestored, ownedMeshesReleased, liveMaterialRebound, gameRuntimeValidated;
        public int checks, articulationPoses, crankSamples;
        public float maximumEndpointGapMetres, minimumTelescopicOverlapMetres;
    }
    public static Report Validate(GameObject prefab)
    {
        var clone = Object.Instantiate(prefab); var nativeOwned = new List<Object>(); S060MeyerExhaustPipe pipe = null; S060SteamPipes supply = null;
        var result = new Report { minimumTelescopicOverlapMetres = float.MaxValue };
        try
        {
            Require(SteamNativePreview.Apply(clone, nativeOwned), "Actual local S060 meshes required for LP return validation.");
            var rear = clone.transform.Find("BogieR/bogie_car"); var front = clone.transform.Find("BogieF/bogie_car");
            var source = clone.GetComponentsInChildren<MeshFilter>(true).Select(f => (f, mesh: f.sharedMesh)).ToArray();
            var nativeBody = source.Single(p => p.f.name == "s060_body").f;
            var sourceTransforms = clone.GetComponentsInChildren<Transform>(true).Select(t => (t, parent: t.parent, pos: t.localPosition, rot: t.localRotation, scale: t.localScale)).ToArray();
            var originalRootPosition = clone.transform.position; var originalRootRotation = clone.transform.rotation;
            supply = S060SteamPipes.Bind(clone.transform, front, rear, clone.transform); pipe = supply.ExhaustReturn;
            Require(pipe != null, "Integrated Meyer steam-pipe owner must bind its LP exhaust return.");
            var generated = pipe.BodyRoot.GetComponentsInChildren<MeshFilter>(true).Concat(pipe.RearRoot.GetComponentsInChildren<MeshFilter>(true)).ToArray();
            var meshes = generated.Select(f => f.sharedMesh).ToArray();
            Require(generated.Length > 10 && generated.All(f => f.sharedMesh != null), "Complete collector/return joint geometry is required."); result.checks++;
            Require(pipe.BodyRoot.GetComponentsInChildren<Collider>(true).Length == 0 && pipe.RearRoot.GetComponentsInChildren<Collider>(true).Length == 0 &&
                pipe.BodyRoot.GetComponentsInChildren<Rigidbody>(true).Length == 0 && pipe.RearRoot.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                "Visual exhaust pipe must not add collision bodies or colliders."); result.checks++;
            result.checks += SteamMeshContact.ValidateAlgorithm();
            var rearHome = rear.localPosition; var frontHome = front.localPosition;
            var gear = source.Where(p => p.mesh != null && (p.f.name.Contains("driver ") ||
                p.f.GetComponentsInParent<Transform>().Any(t => t.name.StartsWith("[Meyer rods ", StringComparison.Ordinal)))).Select(p => p.f).ToArray();
            Require(gear.Length >= 16, "Actual native wheel and authored rod meshes are required."); result.checks++;
            var deck = source.Where(p => p.mesh != null && p.f.GetComponentsInParent<Transform>().Any(t =>
                t.name == "[Attached rear platform]" || t.name == "[Period rear chassis]" || t.name.StartsWith("[Meyer rear pannier ", StringComparison.Ordinal))).Select(p => p.f).ToArray();
            var intake = source.Where(p => p.f.name.StartsWith("[S060 manifold ", StringComparison.Ordinal) || p.f.name.StartsWith("[S060 transfer pipe ", StringComparison.Ordinal)).Select(p => p.f).ToArray();
            Require(deck.Length > 4 && intake.Length == 6, "Native body, authored rear deck and both drawn supply pipe routes are required."); result.checks++;
            var moving = new[] { pipe.InnerSleeve.GetComponent<MeshFilter>(), pipe.OuterSleeve.GetComponent<MeshFilter>(), pipe.SlipCollar.GetComponent<MeshFilter>() };
            float smokeboxSurface = SurfaceX(nativeBody.sharedMesh, 2.25f, 3.7f);
            foreach (float z in new[] { -1f, 0f, 2.75f })
            {
                var support = pipe.BodyRoot.Find("LP body support " + z).GetComponent<MeshFilter>();
                var foot = clone.transform.InverseTransformPoint(DrawnCap(support, false));
                Require(SurfaceYDistance(nativeBody.sharedMesh, foot) < .025f, "Return support foot must meet the actual native plate surface."); result.checks++;
                var collar = pipe.BodyRoot.Find("LP support collar " + z).GetComponent<MeshFilter>();
                Require(new SteamMeshContact.Shell(support, clone.transform).Intersects(new SteamMeshContact.Shell(collar, clone.transform)),
                    "Support must physically meet its pipe collar."); result.checks++;
            }
            foreach (float yaw in new[] { -15f, 0f, 15f }) foreach (float bodyYaw in new[] { 0f, 73f }) foreach (int rerail in new[] { -1, 0, 1 })
            {
                clone.transform.SetPositionAndRotation(new Vector3(120, 20, -73), Quaternion.Euler(rerail * 4, bodyYaw, rerail * -2));
                rear.localPosition = rearHome + Vector3.up * (rerail * .12f); front.localPosition = frontHome - Vector3.up * (rerail * .06f);
                rear.localRotation = Quaternion.Euler(rerail * 3, yaw, rerail * 2);
                front.localRotation = Quaternion.Euler(rerail * -2, -yaw, rerail * -1);
                supply.UpdateGeometry(); result.articulationPoses++;
                var fixedReturn = pipe.BodyRoot.Find("LP smokebox return").GetComponent<MeshFilter>();
                var exterior = fixedReturn.sharedMesh.vertices.Select(v => clone.transform.InverseTransformPoint(fixedReturn.transform.TransformPoint(v))).Where(v => v.z < 3.30f).ToArray();
                Require(exterior.Length > 16 && exterior.All(p => p.x < -1.50f), "Long fixed return must stay outside the actual native body/step envelope (|X|<=1.405m), excluding its intended smokebox entry."); result.checks++;
                var bodyAndDeck = new[] { nativeBody }.Concat(deck).Select(f => new SteamMeshContact.Shell(f, clone.transform)).ToArray();
                var drawnIntakes = intake.Select(f => new SteamMeshContact.Shell(f, clone.transform)).ToArray();
                foreach (var filter in generated)
                {
                    var exhaust = new SteamMeshContact.Shell(filter, clone.transform);
                    foreach (var target in drawnIntakes)
                    { Require(!exhaust.Intersects(target, out var contact), "LP collector/return intersects drawn intake route: " + filter.name + " / " + target.Name + " at yaw " + yaw + ", rerail " + rerail + ". " + contact); result.checks++; }
                }
                foreach (var movingFilter in moving)
                {
                    var shell = new SteamMeshContact.Shell(movingFilter, clone.transform);
                    foreach (var target in bodyAndDeck.Concat(drawnIntakes))
                    { Require(!shell.Intersects(target, out var contact), "Moving LP sleeve intersects native body/step/ashpan, attached deck or supply pipe: " + movingFilter.name + " / " + target.Name + " at yaw " + yaw + ", rerail " + rerail + ". " + contact); result.checks++; }
                }
                foreach (int side in new[] { -1, 1 })
                {
                    var chest = rear.Find("[Round cylinders " + (side < 0 ? "L" : "R") + "]/Valve chest").GetComponent<MeshFilter>();
                    var expected = chest.transform.TransformPoint(LowCapCentre(chest.sharedMesh));
                    var riser = pipe.RearRoot.Find("LP exhaust riser " + side).GetComponent<MeshFilter>();
                    Near(DrawnCap(riser, false), expected);
                    Near(DrawnCap(riser, true), pipe.RearRoot.TransformPoint(new Vector3(side * 1.03f, 1.68f, .85f)));
                }
                Near(DrawnCap(pipe.BodyRoot.Find("LP smokebox return").GetComponent<MeshFilter>(), true), pipe.SmokeboxJoint.position);
                float socketX = clone.transform.InverseTransformPoint(pipe.SmokeboxJoint.position).x;
                Require(socketX >= smokeboxSurface && socketX <= smokeboxSurface + .035f, "Drawn return must enter the actual native smokebox surface without a floating socket."); result.checks++;
                Near(DrawnCap(pipe.InnerSleeve.GetComponent<MeshFilter>(), false), pipe.RearJoint.position);
                Near(DrawnCap(pipe.OuterSleeve.GetComponent<MeshFilter>(), false), pipe.BodyJoint.position);
                var direction = (pipe.BodyJoint.position - pipe.RearJoint.position).normalized;
                var innerEnd = DrawnCap(pipe.InnerSleeve.GetComponent<MeshFilter>(), true);
                var outerEnd = DrawnCap(pipe.OuterSleeve.GetComponent<MeshFilter>(), true);
                float overlap = Vector3.Dot(innerEnd - outerEnd, direction); result.minimumTelescopicOverlapMetres = Mathf.Min(result.minimumTelescopicOverlapMetres, overlap);
                Require(overlap > .10f && overlap < 1.50f && Vector3.Cross(innerEnd - outerEnd, direction).magnitude < .0001f,
                    "Actual rigid sleeve meshes must remain collinear with positive travel margin."); result.checks++;
                Require(Mathf.Abs(Vector3.Distance(innerEnd, pipe.RearJoint.position) - 1.55f) < .0001f &&
                    Mathf.Abs(Vector3.Distance(outerEnd, pipe.BodyJoint.position) - 1.55f) < .0001f,
                    "Telescopic tubes retain their actual fixed 1.55m length."); result.checks++;
                Near(pipe.SlipCollar.position, outerEnd);
                foreach (var joint in new[] { pipe.RearJoint, pipe.BodyJoint })
                {
                    var b = joint.GetComponent<MeshFilter>().sharedMesh.bounds;
                    Require(Vector3.Distance(b.center, Vector3.zero) < .00001f && b.extents.x > .14f && b.extents.y > .14f && b.extents.z > .14f,
                        "Actual ball-joint shell must enclose each full-size tube attachment."); result.checks++;
                }
                foreach (var sleeve in new[] { pipe.InnerSleeve, pipe.OuterSleeve })
                {
                    var f = sleeve.GetComponent<MeshFilter>(); var cap = DrawnCap(f, false);
                    var vertices = f.sharedMesh.vertices;
                    float radius = Vector3.Distance(f.transform.TransformPoint(vertices[0]), cap);
                    Require(Mathf.Abs(radius - (sleeve == pipe.InnerSleeve ? .09f : .11f)) < .0001f,
                        "Articulation must not shrink or stretch tube cross-sections."); result.checks++;
                }
                for (int crank = 0; crank < 4; crank++)
                {
                    rear.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single().SampleAnimation(rear.gameObject, crank / 4f);
                    front.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single().SampleAnimation(front.gameObject, (crank / 4f + .125f) % 1);
                    var gearShells = gear.Select(f => new SteamMeshContact.Shell(f, clone.transform)).ToArray();
                    foreach (var filter in generated)
                    {
                        var shell = new SteamMeshContact.Shell(filter, clone.transform);
                        foreach (var target in gearShells)
                        { Require(!shell.Intersects(target, out var contact), "LP exhaust intersects actual running gear: " + filter.name + " / " + target.Name + " at yaw " + yaw + ", rerail " + rerail + ", crank " + crank + ". " + contact); result.checks++; }
                    }
                    result.crankSamples++;
                }
            }
            // Restore sampled source poses before testing stock removal; this
            // distinguishes the test's own sampling from pipe ownership.
            foreach (var home in sourceTransforms)
            { home.t.localPosition = home.pos; home.t.localRotation = home.rot; home.t.localScale = home.scale; }
            clone.transform.SetPositionAndRotation(originalRootPosition, originalRootRotation);
            var paint = nativeBody.GetComponent<MeshRenderer>(); var oldMaterial = paint.sharedMaterial;
            var alternate = new Material(oldMaterial); nativeOwned.Add(alternate); paint.sharedMaterial = alternate; pipe.UpdateGeometry();
            Require(generated.All(f => f.GetComponent<MeshRenderer>().sharedMaterial == alternate), "All return geometry must follow the current live S060 material."); result.checks++;
            paint.sharedMaterial = oldMaterial; pipe.UpdateGeometry();
            supply.Dispose(); supply.Dispose();
            Require(meshes.All(m => m == null), "Every owned return mesh must be released by idempotent disposal."); result.checks++;
            Require(source.All(p => p.f != null && p.f.sharedMesh == p.mesh), "Return removal must retain all original meshes."); result.checks++;
            Require(sourceTransforms.All(h => h.t != null && h.t.parent == h.parent && h.t.localPosition == h.pos && h.t.localRotation == h.rot && h.t.localScale == h.scale),
                "Returning to stock must preserve all original transform parents/poses/scales."); result.checks++;
            Require(clone.transform.Find("[Meyer LP exhaust return]") == null && rear.Find("[Meyer LP exhaust collector]") == null,
                "Owned collector and return roots must disappear on removal."); result.checks++;
            result.passed = result.actualValveChestAndSmokeboxTriangles = result.actualDrawnCapsAndSleeves = true;
            result.actualRunningGearTriangleIntersections = result.independentBodyAndBogiePoses = true;
            result.movingSleevesClearNativeBodyAndDeck = result.movingSleevesClearSupplyPipes = result.fixedOutboardRunClearNativeEnvelope = true;
            result.allExhaustPartsClearSupplyPipes = true;
            result.supportsMeetActualNativePlate = true;
            result.stockSourcesRestored = result.ownedMeshesReleased = result.liveMaterialRebound = true;
            return result;
            void Near(Vector3 actual, Vector3 expected)
            { float gap = Vector3.Distance(actual, expected); result.maximumEndpointGapMetres = Mathf.Max(result.maximumEndpointGapMetres, gap); Require(gap < .00015f, "Drawn LP return cap detached by " + gap.ToString("R", CultureInfo.InvariantCulture)); result.checks++; }
        }
        finally { supply?.Dispose(); pipe?.Dispose(); Object.DestroyImmediate(clone); foreach (var item in nativeOwned) if (item != null) Object.DestroyImmediate(item); }
    }
    private static Vector3 LowCapCentre(Mesh mesh)
    {
        var v = mesh.vertices; var t = mesh.triangles; float low = v.Min(p => p.y), area = 0; var centre = Vector3.zero;
        for (int i = 0; i < t.Length; i += 3)
        {
            var a = v[t[i]]; var b = v[t[i + 1]]; var c = v[t[i + 2]];
            if (Mathf.Max(Mathf.Abs(a.y - low), Mathf.Abs(b.y - low), Mathf.Abs(c.y - low)) > .00001f) continue;
            float weight = Vector3.Cross(b - a, c - a).magnitude; area += weight; centre += (a + b + c) * (weight / 3);
        }
        Require(area > .001f, "Actual LP valve-chest rear-cap triangles required."); return centre / area;
    }
    private static Vector3 DrawnCap(MeshFilter filter, bool end)
    {
        var mesh = filter.sharedMesh; var v = mesh.vertices; var t = mesh.triangles;
        int cap = v.Length - (end ? 1 : 2); float area = 0; var centre = Vector3.zero;
        for (int i = 0; i < t.Length; i += 3)
        {
            if (t[i] != cap && t[i + 1] != cap && t[i + 2] != cap) continue;
            var a = filter.transform.TransformPoint(v[t[i]]); var b = filter.transform.TransformPoint(v[t[i + 1]]); var c = filter.transform.TransformPoint(v[t[i + 2]]);
            float weight = Vector3.Cross(b - a, c - a).magnitude; area += weight; centre += (a + b + c) * (weight / 3);
        }
        Require(area > .001f, "Actual drawn tube cap triangles required."); return centre / area;
    }
    private static float SurfaceX(Mesh mesh, float y, float z)
    {
        var v = mesh.vertices; var t = mesh.triangles; float found = float.PositiveInfinity;
        for (int i = 0; i < t.Length; i += 3)
        {
            var a = v[t[i]]; var b = v[t[i + 1]]; var c = v[t[i + 2]];
            float denominator = (b.z - c.z) * (a.y - c.y) + (c.y - b.y) * (a.z - c.z); if (Mathf.Abs(denominator) < 1e-10f) continue;
            float u = ((b.z - c.z) * (y - c.y) + (c.y - b.y) * (z - c.z)) / denominator;
            float w = ((c.z - a.z) * (y - c.y) + (a.y - c.y) * (z - c.z)) / denominator;
            if (u < -.00001f || w < -.00001f || u + w > 1.00001f) continue;
            float x = u * a.x + w * b.x + (1 - u - w) * c.x; if (x > -1.1f && x < -.3f) found = Mathf.Min(found, x);
        }
        Require(!float.IsPositiveInfinity(found), "Actual native smokebox surface triangles are required."); return found;
    }
    private static float SurfaceYDistance(Mesh mesh, Vector3 point)
    {
        var v = mesh.vertices; var t = mesh.triangles; float found = float.PositiveInfinity;
        for (int i = 0; i < t.Length; i += 3)
        {
            var a = v[t[i]]; var b = v[t[i + 1]]; var c = v[t[i + 2]];
            float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z); if (Mathf.Abs(d) < 1e-10f) continue;
            float u = ((b.z - c.z) * (point.x - c.x) + (c.x - b.x) * (point.z - c.z)) / d;
            float w = ((c.z - a.z) * (point.x - c.x) + (a.x - c.x) * (point.z - c.z)) / d;
            if (u < -.00001f || w < -.00001f || u + w > 1.00001f) continue;
            found = Mathf.Min(found, Mathf.Abs(u * a.y + w * b.y + (1 - u - w) * c.y - point.y));
        }
        return found;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
