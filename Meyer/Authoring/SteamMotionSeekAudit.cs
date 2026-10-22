using System;
using System.Linq;
using CCL.Types.Proxies.Ports;
using CCL.Types.Proxies.Wheels;
using UnityEngine;

// Verde demonstrates separate native crank and reverser inputs. These models
// currently have crank-driven rods/Franklin shafts, but no variable-cutoff
// linkage to blend. Prove the independent absolute clocks instead of changing
// conrod geometry or borrowing Verde's incompatible armature clips.
public static class SteamMotionSeekAudit
{
    private sealed class Pose
    {
        public Transform[] parts;
        public Vector3[] positions, scales;
        public Quaternion[] rotations;
        public Pose(Transform root)
        {
            parts = root.GetComponentsInChildren<Transform>(true);
            positions = parts.Select(t => t.localPosition).ToArray();
            scales = parts.Select(t => t.localScale).ToArray();
            rotations = parts.Select(t => t.localRotation).ToArray();
        }
        public void RequireUnchanged(string reason, ref int checks)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                Require(Vector3.Distance(parts[i].localPosition, positions[i]) < .0001f &&
                    Vector3.Distance(parts[i].localScale, scales[i]) < .0001f &&
                    Quaternion.Angle(parts[i].localRotation, rotations[i]) < .1f, reason + ": " + parts[i].name);
                checks++;
            }
        }
    }

    public static int Validate(GameObject clone, bool dockside, Func<float, int, Vector3> crosshead)
    {
        int checks = 0;
        var front = clone.transform.Find("BogieF/bogie_car");
        var rear = clone.transform.Find("BogieR/bogie_car");
        Require(front != null && rear != null, "Two physical engine frames are required.");
        var units = new[] { front, rear };
        var readers = clone.GetComponentsInChildren<AnimatorPortReaderProxy>(true);
        Require(readers.Length == 2 && clone.GetComponent<PoweredWheelRotationViaCodeProxy>() == null,
            "Two native absolute clocks must own the wheels without a competing RPM integrator."); checks++;
        var expected = dockside ? new[] { MeyerSteamAuthoring.Lp, MeyerSteamAuthoring.Hp } : new[] { MeyerSteamAuthoring.Hp, MeyerSteamAuthoring.Lp };
        var clips = units.Select(unit => unit.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single()).ToArray();
        for (int unitIndex = 0; unitIndex < units.Length; unitIndex++)
        {
            var unit = units[unitIndex];
            var reader = unit.GetComponents<AnimatorPortReaderProxy>().Single();
            Require(reader.portId == expected[unitIndex] + ".CRANK_ROTATION" &&
                reader.updateType == AnimatorPortReaderProxy.UpdateType.SET_NORMALIZED_TIME && reader.valueMultiplier == 1 && reader.valueOffset == 0,
                "Each physical engine must follow its own native crank, without an extra phase offset."); checks++;
            var clip = clips[unitIndex]; Require(Mathf.Abs(clip.length - 1) < .00001f, "A drive clip must represent exactly one crank turn."); checks++;
            void Sample(float turns)
            {
                // Native AnimatorPortReader.OnValueUpdate uses FloorMod(1).
                // This also models negative direction and multiple revolutions.
                clip.SampleAnimation(unit.gameObject, (turns - Mathf.Floor(turns)) * clip.length);
            }
            foreach (float target in new[] { -.125f, -.00001f, 0f, .00001f, .173f, .5f, .875f, .99999f, 1f, 1.125f, 2.375f })
            {
                Sample(target); var reference = new Pose(unit);
                foreach (float seek in new[] { .9f, -.2f, 1.75f, .1f, -1.01f }) Sample(seek);
                Sample(target); reference.RequireUnchanged("Absolute/reverse/wrapped seeks must not accumulate motion drift", ref checks);
                float turns = target - Mathf.Floor(target);
                foreach (int side in new[] { -1, 1 })
                {
                    string suffix = side < 0 ? "L" : "R";
                    float phase = turns + (side < 0 ? .25f : 0);
                    var rods = unit.Find((dockside ? "[Dockside rods " : "[Meyer rods ") + suffix + "]");
                    var expectedCrosshead = crosshead(phase, side);
                    Require(Vector3.Distance(unit.InverseTransformPoint(rods.Find("Crosshead").position), expectedCrosshead) < .00035f,
                        "Reversed/wrapped crank seeks must keep each side on the analytic piston phase."); checks++;
                }
            }
            // Sample the actual endpoints too: wrapping through zero must be
            // continuous, including the source Franklin 2:1 shaft rotations.
            clip.SampleAnimation(unit.gameObject, 0); var zero = new Pose(unit);
            clip.SampleAnimation(unit.gameObject, clip.length);
            zero.RequireUnchanged("One-turn clip endpoints must close", ref checks);
            clip.SampleAnimation(unit.gameObject, .99999f); var beforeSeam = new Pose(unit);
            clip.SampleAnimation(unit.gameObject, .00001f);
            for (int i = 0; i < beforeSeam.parts.Length; i++)
            {
                Require(Vector3.Distance(beforeSeam.parts[i].localPosition, beforeSeam.positions[i]) < .0006f &&
                    Vector3.Distance(beforeSeam.parts[i].localScale, beforeSeam.scales[i]) < .0006f &&
                    Quaternion.Angle(beforeSeam.parts[i].localRotation, beforeSeam.rotations[i]) < .1f,
                    "The wrap seam must remain continuous in both directions: " + beforeSeam.parts[i].name); checks++;
            }
        }
        // Deliberately drive unrelated phases. A shared clip/controller asset
        // must not make one unit sample or overwrite the other unit's pose.
        clips[0].SampleAnimation(front.gameObject, .173f);
        clips[1].SampleAnimation(rear.gameObject, .643f);
        var rearSnapshot = new Pose(rear);
        foreach (float phase in new[] { .9f, .125f, .875f, 0 })
        {
            clips[0].SampleAnimation(front.gameObject, phase);
            rearSnapshot.RequireUnchanged("Sampling the front engine must leave the independently phased rear engine untouched", ref checks);
        }
        var frontSnapshot = new Pose(front);
        foreach (float phase in new[] { .2f, .75f, .01f, 1 })
        {
            clips[1].SampleAnimation(rear.gameObject, phase);
            frontSnapshot.RequireUnchanged("Sampling the rear engine must leave the independently phased front engine untouched", ref checks);
        }
        return checks;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
