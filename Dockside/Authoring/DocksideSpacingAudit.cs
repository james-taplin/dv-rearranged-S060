using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CCL.Types;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Measure spacing using the validated model and existing exact native preview
// meshes. No production asset is changed by a trial.
public static class DocksideSpacingAudit
{
    [Serializable] private sealed class Fit
    {
        public float centre, minimumLongitudinalGap, maximumBodyAbsZ;
        public int poses; public bool passes;
    }
    [Serializable] private sealed class Report
    {
        public bool passed, actualNativeWheels = true, syntheticSupportYaw = true, gameLaunched = false;
        public Fit[] fits;
        public bool actualSerializedPositions, measuredNativeSupportPivots = true;
        public float[] serializedAxleCentresZ;
    }
    private static IEnumerable<Vector3> Points(MeshFilter filter, Transform space)
    {
        var matrix = space.worldToLocalMatrix * filter.transform.localToWorldMatrix;
        var mesh = filter.sharedMesh;
        if (mesh.isReadable) return mesh.vertices.Select(matrix.MultiplyPoint3x4);
        var b = mesh.bounds; var points = new List<Vector3>();
        foreach (float x in new[] { b.min.x, b.max.x }) foreach (float y in new[] { b.min.y, b.max.y }) foreach (float z in new[] { b.min.z, b.max.z })
            points.Add(matrix.MultiplyPoint3x4(new Vector3(x, y, z)));
        return points;
    }
    public static void Validate(GameObject prefab, string output, bool trials)
    {
        var root = Object.Instantiate(prefab); var owned = new List<Object>(); var fits = new List<Fit>();
        try
        {
            if (!SteamNativePreview.Apply(root, owned)) throw new InvalidOperationException("Exact native wheel fixture is required for spacing.");
            var front = root.transform.Find("BogieF/bogie_car"); var rear = root.transform.Find("BogieR/bogie_car");
            var frontMeshes = front.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null && !f.sharedMesh.name.StartsWith("Steam", StringComparison.Ordinal) && f.GetComponent<Renderer>()?.enabled == true).ToArray();
            var rearMeshes = rear.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null && !f.sharedMesh.name.StartsWith("Steam", StringComparison.Ordinal) && f.GetComponent<Renderer>()?.enabled == true).ToArray();
            var clip = front.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
            var axleZ = front.Cast<Transform>().Concat(rear.Cast<Transform>()).Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).Select(t => root.transform.InverseTransformPoint(t.position).z).OrderBy(z => z).ToArray();
            if (!trials)
            {
                var expected = new[] { -DocksidePlan.BogieCentre - DocksidePlan.AxlePitch / 2, -DocksidePlan.BogieCentre + DocksidePlan.AxlePitch / 2, DocksidePlan.BogieCentre - DocksidePlan.AxlePitch / 2, DocksidePlan.BogieCentre + DocksidePlan.AxlePitch / 2 };
                if (axleZ.Length != 4 || axleZ.Where((z, i) => Mathf.Abs(z - expected[i]) > .00001f).Any())
                    throw new InvalidOperationException("Serialized wheelbase differs from the authored wider layout.");
            }
            var centres = trials ? new[] { 1.4f, 1.75f, 1.76f } : new[] { DocksidePlan.BogieCentre };
            foreach (float centre in centres)
            {
                if (trials)
                {
                    root.transform.Find("BogieF").localPosition = new Vector3(0, 0, centre);
                    root.transform.Find("BogieR").localPosition = new Vector3(0, 0, -centre);
                }
                var fit = new Fit { centre = centre, minimumLongitudinalGap = float.PositiveInfinity };
                foreach (float yaw in new[] { -12f, -2f, 0, 2f, 12f })
                {
                    SteamBogiePoseAudit.Pose(root.transform, front, new Vector3(0, 0, centre), true, yaw);
                    SteamBogiePoseAudit.Pose(root.transform, rear, new Vector3(0, 0, -centre), false, -yaw);
                    // Mechanical-unit Z separation covers all independent phase pairs.
                    // Deliberate boiler/transfer pipes are tested by SteamPipeAudit:
                    // Collect each unit's extrema over the complete revolution.
                    float frontMin = float.PositiveInfinity, rearMax = float.NegativeInfinity;
                    for (int phase = 0; phase < 128; phase++)
                    {
                        clip.SampleAnimation(front.gameObject, phase / 128f);
                        clip.SampleAnimation(rear.gameObject, phase / 128f);
                        foreach (var f in frontMeshes) foreach (var point in Points(f, root.transform))
                        { frontMin = Mathf.Min(frontMin, point.z); fit.maximumBodyAbsZ = Mathf.Max(fit.maximumBodyAbsZ, Mathf.Abs(point.z)); }
                        foreach (var f in rearMeshes) foreach (var point in Points(f, root.transform))
                        { rearMax = Mathf.Max(rearMax, point.z); fit.maximumBodyAbsZ = Mathf.Max(fit.maximumBodyAbsZ, Mathf.Abs(point.z)); }
                        fit.poses++;
                    }
                    fit.minimumLongitudinalGap = Mathf.Min(fit.minimumLongitudinalGap, frontMin - rearMax);
                }
                fit.passes = fit.minimumLongitudinalGap >= .020f && fit.maximumBodyAbsZ <= 3.980f;
                fits.Add(fit);
            }
            var report = new Report { passed = trials ? fits.Any(f => f.passes) : fits.All(f => f.passes), fits = fits.ToArray(), actualSerializedPositions = !trials, serializedAxleCentresZ = axleZ };
            File.WriteAllText(Path.Combine(output, "spacing-validation.json"), JsonUtility.ToJson(report, true));
            if (!report.passed) throw new InvalidOperationException("No measured spacing meets the 20 mm engine gap and retained body length.");
        }
        finally { Object.DestroyImmediate(root); foreach (var item in owned) if (item != null) Object.DestroyImmediate(item); }
    }
    public static void RunFinal() => Run(false);
    public static void RunTrials() => Run(true);
    private static void Run(bool trials)
    {
        string output = Environment.GetEnvironmentVariable("DOCKSIDE_OUTPUT"); Directory.CreateDirectory(output);
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("DOCKSIDE_AUDIT_BUNDLE"));
        try { Validate(bundle.LoadAllAssets<CustomCarPack>().Single().Cars[0].liveries[0].prefab, output, trials); EditorApplication.Exit(0); }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "spacing-error.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); }
        finally { bundle.Unload(true); }
    }
}
