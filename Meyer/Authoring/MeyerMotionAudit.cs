using System;
using System.IO;
using System.Linq;
using CCL.Types.Proxies.Ports;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MeyerMotionAudit
{
    public static void Validate(GameObject prefab, string output)
    {
        var clone = Object.Instantiate(prefab); int checks = 0; float max = 0;
        try
        {
            var readers = clone.GetComponentsInChildren<AnimatorPortReaderProxy>(true);
            Require(readers.Length == 2, "Meyer needs two native crank readers.");
            foreach (float yaw in new[] { -12f, 0, 12f }) foreach (var reader in readers)
            {
                var unit = reader.transform; unit.localRotation = Quaternion.Euler(0, reader.portId.StartsWith(MeyerSteamAuthoring.Lp) ? yaw : -yaw, 0);
                Require(unit.Find("[Powered bogie frame]") != null, "Missing visible Meyer power frame.");
                foreach (string suffix in new[] { "L", "R" }) Require(unit.Find("[Round cylinders " + suffix + "]/Steam cylinder barrel") != null, "Missing round Meyer cylinder.");
                var clip = reader.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
                for (int sample = 0; sample <= 128; sample++)
                {
                    float t = sample / 128f; clip.SampleAnimation(unit.gameObject, t);
                    foreach (int side in new[] { -1, 1 })
                    {
                        float phase = t + (side < 0 ? .25f : 0); string suffix = side < 0 ? "L" : "R";
                        var rods = unit.Find("[Meyer rods " + suffix + "]");
                        var piston = rods.Find("Piston rod");
                        Require(Mathf.Abs(piston.localScale.x - .035f) < .00001f && Mathf.Abs(piston.localScale.y - .035f) < .00001f, "Partial scale curves must retain the piston rod cross-section."); checks++;
                        Vector3 P(Transform part, Vector3 p) => unit.InverseTransformPoint(part.TransformPoint(p));
                        void Near(Vector3 actual, Vector3 expected)
                        { float error = Vector3.Distance(actual, expected); max = Mathf.Max(max, error); Require(error < .00035f, "Meyer rod detached: " + error); checks++; }
                        var pin = MeyerMotionAuthoring.Pin(phase, side, false); var front = MeyerMotionAuthoring.Pin(phase, side, true);
                        var main = MeyerMotionAuthoring.MainPin(phase, side); var cross = MeyerMotionAuthoring.Crosshead(phase, side);
                        Near(P(rods.Find("Coupling rod"), new Vector3(0, 0, -.5f)), pin);
                        Near(P(rods.Find("Coupling rod"), new Vector3(0, 0, .5f)), front);
                        Near(P(rods.Find("Main rod"), new Vector3(0, 0, -.5f)), main);
                        Near(P(rods.Find("Main rod"), new Vector3(0, 0, .5f)), cross);
                        Near(P(rods.Find("Piston rod"), new Vector3(0, 0, -.5f)), MeyerMotionAuthoring.PistonCrosshead(phase, side));
                        Near(P(rods.Find("Piston rod"), new Vector3(0, 0, .5f)), new Vector3(side * 1.075f, (float)MeyerPlan.DriverRadius, MeyerPlan.CylinderZ + .05f));
                        foreach (bool first in new[] { false, true })
                        {
                            var wheel = unit.Find((first ? "[axle] front" : "[axle] rear") + "/S060 driver " + suffix + " (native alias)");
                            var actual = P(wheel, new Vector3(.98730177f, -MeyerPlan.CrankRadius, 0));
                            var expected = first ? front : pin;
                            // X bearing fit is measured against actual triangles by SteamNativePinAudit.
                            Require(Vector2.Distance(new Vector2(actual.y, actual.z), new Vector2(expected.y, expected.z)) < .001f, "Native wheel crank Y/Z and Meyer rod disagree."); checks++;
                        }
                        // Measured local native S060 front-gland rear plane after
                        // translation is .780774m. Test the actual crosshead box
                        // front edge; the old centre-only .94 bound hid clipping.
                        float crossFront = P(rods.Find("Crosshead"), new Vector3(0, 0, .5f)).z;
                        Require(crossFront < .780774f - .025f, "Meyer crosshead front edge collides with native cylinder gland."); checks++;
                        float guideRear = .31f - .90f / 2, guideFront = .31f + .90f / 2;
                        Require(cross.z - .065f >= guideRear && crossFront <= guideFront, "Meyer crosshead leaves its guide rails."); checks++;
                    }
                }
            }
            SteamBogiePreview.Render(clone, output, "meyer", (float)MeyerPlan.DriverRadius, "S060 driver ");
            File.WriteAllText(Path.Combine(output, "motion-validation.json"), "{\"passed\":true,\"poseAssertions\":" + checks + ",\"visibleFrames\":2,\"roundCylinders\":4,\"nativeWheelPinChecked\":true,\"yawDegrees\":[-12,0,12],\"maxEndpointErrorMetres\":" + max.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"gameRuntimeValidated\":false}");
        }
        finally { Object.DestroyImmediate(clone); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
