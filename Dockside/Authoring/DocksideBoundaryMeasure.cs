using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

// Bounded measurement of the existing close engine boundary. This deliberately
// reports inherited contacts rather than changing wheelbase or cylinder layout.
public static class DocksideBoundaryMeasure
{
    [Serializable] private sealed class Result
    {
        public bool passed;
        public int poses, existingRodContactPoses, revisedRodContactPoses, crankContactPoses;
        public string firstContact;
        public bool actualTriangles = true, syntheticSupportYaw = true, gameplayValidated = false;
    }
    public static void Measure(GameObject prefab, string output, bool requireClearance = false)
    {
        var root = Object.Instantiate(prefab); var oldMesh = RearrangedS060.S060RodMesh.Normalized("Baseline Dockside main rod QA", DocksidePlan.MainRodLength);
        var result = new Result();
        try
        {
            var front = root.transform.Find("BogieF/bogie_car"); var rear = root.transform.Find("BogieR/bogie_car");
            foreach (float yaw in new[] { -12f, -2f, 0, 2f, 12f }) for (int sample = 0; sample < 32; sample++)
            {
                SteamBogiePoseAudit.Pose(root.transform, front, new Vector3(0, 0, DocksidePlan.BogieCentre), true, yaw);
                SteamBogiePoseAudit.Pose(root.transform, rear, new Vector3(0, 0, -DocksidePlan.BogieCentre), false, -yaw);
                var clip = front.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single(); clip.SampleAnimation(front.gameObject, sample / 32f + .125f);
                foreach (string side in new[] { "L", "R" })
                {
                    var main = front.Find("[Dockside rods " + side + "]/Main rod").GetComponent<MeshFilter>();
                    var crank = front.Find("Franklin return crank " + side + "/Wheel crank").GetComponent<MeshFilter>();
                    var targets = rear.Find("[Round cylinders " + side + "]").GetComponentsInChildren<MeshFilter>().Where(f => f.name == "Steam cylinder barrel" || f.name == "Front cylinder cover" || f.name == "Front cylinder flange").Select(f => new SteamMeshContact.Shell(f, root.transform)).ToArray();
                    var current = new SteamMeshContact.Shell(main, root.transform); var crankShell = new SteamMeshContact.Shell(crank, root.transform);
                    var saved = main.sharedMesh; main.sharedMesh = oldMesh; var baseline = new SteamMeshContact.Shell(main, root.transform); main.sharedMesh = saved;
                    bool oldHit = false, newHit = false, crankHit = false;
                    foreach (var target in targets)
                    {
                        oldHit |= baseline.Intersects(target); newHit |= current.Intersects(target);
                        if (crankShell.Intersects(target, out string witness)) { crankHit = true; if (result.firstContact == null) result.firstContact = "yaw=" + yaw + ", phase=" + sample / 32f + ", side=" + side + ": " + witness; }
                    }
                    result.poses++; if (oldHit) result.existingRodContactPoses++; if (newHit) result.revisedRodContactPoses++; if (crankHit) result.crankContactPoses++;
                }
            }
            result.passed = result.revisedRodContactPoses == 0 && result.crankContactPoses == 0;
            File.WriteAllText(Path.Combine(output, requireClearance ? "boundary-validation.json" : "inter-unit-measurement.json"), JsonUtility.ToJson(result, true));
            if (requireClearance && !result.passed) throw new InvalidOperationException("The wider Dockside still has measured rod/crank-to-cylinder contacts.");
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(oldMesh); }
    }
}
