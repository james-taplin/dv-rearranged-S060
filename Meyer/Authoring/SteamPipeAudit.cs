using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using RearrangedS060;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SteamPipeAudit
{
    public static void Validate(GameObject prefab, string output)
    {
        var clone = Object.Instantiate(prefab); int checks = 0; float max = 0; var owned = new List<Object>(); S060SteamPipes owner = null;
        try
        {
            var front = clone.transform.Find("BogieF/bogie_car"); var rear = clone.transform.Find("BogieR/bogie_car");
            var native = SteamNativePreview.CreatePipeFixture(clone.transform, owned);
            var caps = new Dictionary<string, int[]>();
            foreach (string side in new[] { "L", "R" })
            {
                var part = front.Find("[S060 manifold " + side + "]"); var mesh = part.GetComponent<MeshFilter>().sharedMesh;
                var datum = part.Find("[boiler datum]"); var normal = datum.localRotation * Vector3.up;
                var vertices = mesh.vertices; var triangles = mesh.triangles; var faces = new List<int>();
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
                    var cross = Vector3.Cross(b - a, c - a);
                    if (cross.sqrMagnitude > 1e-12f && Vector3.Dot(cross.normalized, normal) > .9999f &&
                        Mathf.Abs(Vector3.Dot((a + b + c) / 3 - datum.localPosition, normal)) < .0002f) faces.Add(i);
                }
                Require(faces.Count > 10, "Actual front-manifold cap triangles must be present with outward winding."); checks++;
                caps.Add(side, faces.ToArray());
            }
            var sources = clone.GetComponentsInChildren<MeshFilter>(true).Select(f => (f, f.sharedMesh)).ToArray();
            owner = S060SteamPipes.Bind(clone.transform, front, rear, native);
            foreach (float yaw in new[] { -15f, -8f, 0, 8f, 15f })
                foreach (float bodyYaw in new[] { 0, 73f })
                {
                    clone.transform.SetPositionAndRotation(new Vector3(120, 20, -73), Quaternion.Euler(0, bodyYaw, 0));
                    front.localRotation = Quaternion.Euler(0, yaw, 0); rear.localRotation = Quaternion.Euler(0, -yaw, 0);
                    owner.UpdateGeometry();
                    foreach (string side in new[] { "L", "R" })
                    {
                        var manifold = front.Find("[S060 manifold " + side + "]"); var fitted = manifold.GetComponent<MeshFilter>().sharedMesh;
                        var capVertices = fitted.vertices; var triangles = fitted.triangles; Vector3 centre = Vector3.zero; float area = 0;
                        var target = clone.transform.TransformPoint(S060PipeGeometry.BoilerSocket(side == "L" ? -1 : 1));
                        foreach (int face in caps[side])
                        {
                            var ca = manifold.TransformPoint(capVertices[triangles[face]]); var cb = manifold.TransformPoint(capVertices[triangles[face + 1]]); var cc = manifold.TransformPoint(capVertices[triangles[face + 2]]);
                            var cross = Vector3.Cross(cb - ca, cc - ca); float size = cross.magnitude;
                            Require(Vector3.Dot(cross.normalized, clone.transform.up) > .9999f, "Actual boiler cap must retain outward winding and mate to the native neck plane."); checks++;
                            centre += (ca + cb + cc) * (size / 3); area += size;
                        }
                        float capError = Vector3.Distance(centre / area, target); max = Mathf.Max(max, capError);
                        Require(capError < .0003f, "Actual front boiler mesh cap detached from native neck: " + capError); checks++;
                        var pipe = front.Find("[S060 transfer pipe " + side + "]"); var v = pipe.GetComponent<MeshFilter>().sharedMesh.vertices;
                        var a = rear.Find("[S060 manifold " + side + "]/[transfer socket]").position;
                        var b = front.Find("[S060 manifold " + side + "]/[transfer socket]").position;
                        var rearManifold = rear.Find("[S060 manifold " + side + "]");
                        var actualRear = rearManifold.TransformPoint(SteamPipeAuthoring.FaceCentre(rearManifold.GetComponent<MeshFilter>().sharedMesh, Vector3.forward));
                        var actualFront = manifold.TransformPoint(SteamPipeAuthoring.FaceCentre(fitted, Vector3.back));
                        var pipeMesh = pipe.GetComponent<MeshFilter>().sharedMesh;
                        var pipeRear = pipe.TransformPoint(SteamPipeAuthoring.FaceCentre(pipeMesh, Vector3.back));
                        var pipeFront = pipe.TransformPoint(SteamPipeAuthoring.FaceCentre(pipeMesh, Vector3.forward));
                        float drawnGap = Mathf.Max(Vector3.Distance(actualRear, pipeRear), Vector3.Distance(actualFront, pipeFront));
                        max = Mathf.Max(max, drawnGap); Require(drawnGap < .0003f, "Actual manifold/transfer mesh caps detached: " + drawnGap); checks++;
                        foreach (var pair in new[] { (actual: pipe.TransformPoint(new Vector3(0, 0, v.Min(p => p.z))), expected: a),
                            (actual: pipe.TransformPoint(new Vector3(0, 0, v.Max(p => p.z))), expected: b) })
                        { float error = Vector3.Distance(pair.actual, pair.expected); max = Mathf.Max(max, error); Require(error < .0003f, "Moving steam-pipe endpoint detached: " + error); checks++; }
                        Require(Mathf.Abs(v.Max(p => p.x) - v.Min(p => p.x) - .2794f * S060PipeGeometry.Radial) < .0001f, "Pipe bore must not stretch with engine separation."); checks++;
                        Require(pipe.localScale == Vector3.one, "Transfer pipe must preserve radial dimensions."); checks++;
                        foreach (var unit in new[] { front, rear })
                        {
                            var lowerPart = unit.Find("[S060 manifold " + side + "]"); var lowerMesh = lowerPart.GetComponent<MeshFilter>().sharedMesh;
                            foreach (var foot in LowerFeet(lowerMesh, lowerPart.Find("[lower datum]").localPosition.z))
                            {
                                float surface;
                                if (unit.Find("[Native S060 cylinders]") != null)
                                {
                                    var point = foot + new Vector3(0, 0, S060PipeGeometry.NativeCylinderCentreZ - MeyerPlan.CylinderZ);
                                    surface = TopSurface(native.GetComponent<MeshFilter>().sharedMesh, point, Matrix4x4.identity, side == "L" ? -1 : 1);
                                }
                                else
                                {
                                    var chest = unit.Find("[Round cylinders " + side + "]/Valve chest").GetComponent<MeshFilter>();
                                    surface = TopSurface(chest.sharedMesh, foot, unit.worldToLocalMatrix * chest.transform.localToWorldMatrix, 0);
                                }
                                Require(foot.y <= surface + .0005f && foot.y > surface - .05f, "Actual manifold foot must intersect the cylinder casting, not float above it."); checks++;
                            }
                        }
                    }
                }
            owner.Dispose();
            Require(sources.All(p => p.f == null || p.f.sharedMesh == p.sharedMesh), "Disposal must restore every original source mesh."); checks++;
            string returnReport = "null";
            if (rear.Find("[Attached rear platform]") != null)
            { var exhaustReturn = MeyerExhaustPipeAudit.Validate(prefab); checks += exhaustReturn.checks; returnReport = JsonUtility.ToJson(exhaustReturn); }
            File.WriteAllText(Path.Combine(output, "steam-pipe-validation.json"), "{\"passed\":true,\"productionPipeChecks\":" + checks + ",\"meyerLpExhaustReturn\":" + returnReport + ",\"source\":\"S282 branch_pipe: front/rear manifolds and dry_pipe_new\",\"yawDegrees\":[-15,-8,0,8,15],\"maxEndpointErrorMetres\":" + max.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"liveS060MaterialAliases\":true,\"gameRuntimeValidated\":false}");
        }
        finally { owner?.Dispose(); Object.DestroyImmediate(clone); foreach (var value in owned) Object.DestroyImmediate(value); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static Vector3[] LowerFeet(Mesh mesh, float middle)
    {
        var vertices = mesh.vertices; var triangles = mesh.triangles; float low = vertices.Min(p => p.y); var result = new List<Vector3>();
        foreach (int sign in new[] { -1, 1 })
        {
            Vector3 centre = Vector3.zero; float area = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
                if (Mathf.Max(Mathf.Abs(a.y - low), Mathf.Abs(b.y - low), Mathf.Abs(c.y - low)) > .0002f || ((a.z + b.z + c.z) / 3 - middle) * sign <= 0) continue;
                var cross = Vector3.Cross(b - a, c - a); float size = cross.magnitude;
                if (size < 1e-8f || Vector3.Dot(cross / size, Vector3.down) < .9999f) continue;
                centre += (a + b + c) * (size / 3); area += size;
            }
            Require(area > .001f, "Actual manifold bottom flange faces are missing."); result.Add(centre / area);
        }
        return result.ToArray();
    }
    private static float TopSurface(Mesh mesh, Vector3 point, Matrix4x4 matrix, int nativeSide)
    {
        var vertices = mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray(); var triangles = mesh.triangles; float top = float.NegativeInfinity;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
            if (nativeSide != 0 && !(SteamNativePreview.NativeCylinder(a, nativeSide) && SteamNativePreview.NativeCylinder(b, nativeSide) && SteamNativePreview.NativeCylinder(c, nativeSide))) continue;
            float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Mathf.Abs(denominator) < 1e-9f) continue;
            float u = ((b.z - c.z) * (point.x - c.x) + (c.x - b.x) * (point.z - c.z)) / denominator;
            float v = ((c.z - a.z) * (point.x - c.x) + (a.x - c.x) * (point.z - c.z)) / denominator;
            float w = 1 - u - v;
            if (u >= -.0001f && v >= -.0001f && w >= -.0001f) top = Mathf.Max(top, u * a.y + v * b.y + w * c.y);
        }
        Require(!float.IsNegativeInfinity(top), "No actual cylinder casting underneath manifold foot."); return top;
    }
}
