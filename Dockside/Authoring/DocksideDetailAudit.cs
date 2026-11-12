using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CCL.Types.Proxies.Ports;
using UnityEngine;
using Object = UnityEngine.Object;

// Focused reopened-pack checks; numerical and offline visual evidence only.
public static class DocksideDetailAudit
{
    private static int checks;
    private static void Require(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
    private static string Key(Vector3 p) => Mathf.RoundToInt(p.x * 1000000) + "," + Mathf.RoundToInt(p.y * 1000000) + "," + Mathf.RoundToInt(p.z * 1000000);
    private static void MeshCheck(Mesh mesh, bool closed)
    {
        Require(mesh.isReadable, "Detailed authored mesh must be readable: " + mesh.name);
        var v = mesh.vertices; var n = mesh.normals; var uv = mesh.uv; var tangents = mesh.tangents; var triangles = mesh.triangles;
        Require(n.Length == v.Length && tangents.Length == v.Length && uv.Length == v.Length, "Missing detailed mesh attributes.");
        var edges = new Dictionary<string, int>();
        void Edge(int a, int b) { string x = Key(v[a]), y = Key(v[b]); string key = string.CompareOrdinal(x, y) < 0 ? x + "/" + y : y + "/" + x; edges[key] = edges.TryGetValue(key, out var count) ? count + 1 : 1; }
        // Exact native S060 main-rod atlas triangle 161/158/160 from the local
        // native-preview fixture; test containment rather than matching literals.
        Vector2 aUv = new Vector2(.8796523213f, .5665826797f), bUv = new Vector2(.8651770949f, .8173931837f), cUv = new Vector2(.8794529438f, .8175566196f);
        float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        float area = Cross(bUv - aUv, cUv - aUv);
        for (int i = 0; i < v.Length; i++)
        {
            float b = Cross(uv[i] - aUv, cUv - aUv) / area, c = Cross(bUv - aUv, uv[i] - aUv) / area;
            Require(b > 0 && c > 0 && b + c < 1, "Generated UV leaves native S060 rod face: " + mesh.name);
            Require(Mathf.Abs(n[i].magnitude - 1) < .001f, "Invalid detailed mesh normal.");
        }
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            var normal = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            Require(normal.sqrMagnitude > 1e-14f && Vector3.Dot(normal, n[a] + n[b] + n[c]) > 0, "Degenerate or inward detailed triangle: " + mesh.name + "/" + i / 3);
            if (closed) { Edge(a, b); Edge(b, c); Edge(c, a); }
        }
        if (closed) foreach (var edge in edges) Require(edge.Value == 2, "Open detailed skin: " + mesh.name + " " + edge.Key + " count=" + edge.Value);
    }
    public static void Validate(GameObject root, string output)
    {
        checks = 0; float maxPinError = 0, minimumBoxGap = float.PositiveInfinity;
        var clone = Object.Instantiate(root);
        try
        {
            var meshes = clone.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).Where(m => m != null && (m.name.StartsWith("DocksideDetail") || m.name.StartsWith("DocksideRod"))).Distinct().ToArray();
            Require(meshes.Length >= 10, "Detailed Franklin geometry missing (old pack is a required negative control).");
            foreach (var mesh in meshes) MeshCheck(mesh, mesh.name != "DocksideDetailCrosshead");
            foreach (var reader in clone.GetComponentsInChildren<AnimatorPortReaderProxy>(true))
            {
                var unit = reader.transform; var clip = unit.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
                foreach (int side in new[] { -1, 1 })
                {
                    string suffix = side < 0 ? "L" : "R";
                    var crank = unit.Find("Franklin return crank " + suffix);
                    Require(crank != null && crank.Find("Wheel crank") != null && crank.Find("Input shaft") != null && crank.Find("Hub sleeve") != null, "Complete wheel input missing.");
                    float gap = DocksideDriveDetail.GearboxFace(unit, unit.Find("Franklin fit " + suffix), side) - DocksideDriveDetail.CrankOutside;
                    minimumBoxGap = Mathf.Min(gap, minimumBoxGap); Require(gap >= .0008f, "Crank plate contacts gearbox cover.");
                    var rods = unit.Find("[Dockside rods " + suffix + "]");
                    var main = rods.Find("Main rod"); var slider = rods.Find("Crosshead");
                    Require(slider.GetComponent<MeshFilter>().sharedMesh.name == "DocksideDetailCrosshead", "Forked crosshead missing.");
                    Require(rods.Find("Piston rod").GetComponent<MeshFilter>().sharedMesh.name == "DocksideDetailPiston", "Round piston rod missing.");
                    Require(Mathf.Abs(main.GetComponent<MeshFilter>().sharedMesh.bounds.size.x - .048f) < .00001f, "Main rod bearing width changed.");
                    foreach (float yaw in new[] { -12f, 0, 12f }) for (int sample = 0; sample <= 128; sample++)
                    {
                        unit.localRotation = Quaternion.Euler(0, yaw, 0);
                        float turns = sample / 128f; clip.SampleAnimation(unit.gameObject, turns);
                        float phase = turns + (side < 0 ? .25f : 0);
                        var expected = DocksideMotionAuthoring.Pin(phase, side, false);
                        var actual = unit.InverseTransformPoint(crank.TransformPoint(new Vector3(0, 0, DocksidePlan.CrankRadius)));
                        float error = Vector2.Distance(new Vector2(actual.y, actual.z), new Vector2(expected.y, expected.z));
                        maxPinError = Mathf.Max(maxPinError, error); Require(error < .00035f, "Return crank leaves actual wheel-pin orbit.");
                        var cross = unit.InverseTransformPoint(slider.position);
                        var smallPin = unit.InverseTransformPoint(rods.Find("Crosshead bearing pin").position);
                        Require(Vector3.Distance(cross, smallPin) < .00001f, "Small-end pin detached from crosshead.");
                        if (yaw == 0 && sample % 8 == 0)
                        {
                            var rodShell = new SteamMeshContact.Shell(main.GetComponent<MeshFilter>(), unit);
                            Require(!rodShell.Intersects(new SteamMeshContact.Shell(slider.GetComponent<MeshFilter>(), unit)), "Main rod intersects crosshead fork.");
                            foreach (var guide in rods.Cast<Transform>().Where(t => t.name.EndsWith("crosshead guide", StringComparison.Ordinal)))
                                Require(!rodShell.Intersects(new SteamMeshContact.Shell(guide.GetComponent<MeshFilter>(), unit)), "Main rod intersects guide.");
                        }
                    }
                }
            }
            File.WriteAllText(Path.Combine(output, "franklin-detail-validation.json"), JsonUtility.ToJson(new Result { passed = true, checks = checks, meshes = meshes.Length, maxCrankPinErrorMetres = maxPinError, minimumGearboxGapMetres = minimumBoxGap }, true));
        }
        finally { Object.DestroyImmediate(clone); }
    }
    [Serializable] private sealed class Result
    {
        public bool passed; public int checks, meshes;
        public float maxCrankPinErrorMetres, minimumGearboxGapMetres;
        public bool nativeAbsoluteCrank = true, authoredGeometryOnly = true, gameplayValidated = false;
    }
}
