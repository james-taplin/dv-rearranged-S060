using System;
using System.IO;
using System.Linq;
using CCL.Types.Proxies.Ports;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DocksideMotionAudit
{
    public static void Validate(GameObject prefab, string output)
    {
        var readers = prefab.GetComponentsInChildren<AnimatorPortReaderProxy>(true);
        Require(readers.Length == 2 && readers.All(r => r.updateType == AnimatorPortReaderProxy.UpdateType.SET_NORMALIZED_TIME && r.valueMultiplier == 1 && r.valueOffset == 0), "Two native absolute crank readers required.");
        Require(readers.Select(r => r.portId).Distinct().Count() == 2, "Each engine must animate from its own crank port.");
        var clone = Object.Instantiate(prefab);
        try
        {
            int assertions = 0; float maxError = 0; float sweptX = 0;
            foreach (float yaw in new[] { -12f, 0, 12f })
                foreach (var reader in clone.GetComponentsInChildren<AnimatorPortReaderProxy>(true))
                {
                    var unit = reader.transform; bool front = reader.portId.StartsWith(MeyerSteamAuthoring.Lp + ".", StringComparison.Ordinal);
                    unit.localRotation = Quaternion.Euler(0, front ? yaw : -yaw, 0);
                    var animator = unit.GetComponent<Animator>();
                    Require(animator != null && !animator.applyRootMotion && animator.cullingMode == AnimatorCullingMode.AlwaysAnimate, "Native motion Animator setup invalid.");
                    var clip = animator.runtimeAnimatorController.animationClips.Single();
                    var shafts = new[] { "cylinder/driveshaft_f", "cylinder/u_joint_mapped", "transmission_box/u_joint", "driveshaft_r", "driveshaft_m" };
                    var atRest = shafts.Select(p => unit.Find("Franklin fit R/" + p).localRotation).ToArray();
                    clip.SampleAnimation(unit.gameObject, .0625f);
                    Require(shafts.Where((p, i) => Quaternion.Angle(atRest[i], unit.Find("Franklin fit R/" + p).localRotation) > 1).Count() == shafts.Length, "All five Franklin shafts/joints must move.");
                    for (int sample = 0; sample <= 128; sample++)
                    {
                        float crank = Mathf.Repeat(sample / 128f + (front ? .125f : 0), 1);
                        clip.SampleAnimation(unit.gameObject, crank);
                        foreach (int side in new[] { 1, -1 })
                        {
                            float phase = crank + (side < 0 ? .25f : 0); string suffix = side > 0 ? "R" : "L";
                            var rods = unit.Find("[Dockside rods " + suffix + "]");
                            var pin = DocksideMotionAuthoring.Pin(phase, side, false);
                            var frontPin = DocksideMotionAuthoring.Pin(phase, side, true);
                            var mainPin = DocksideMotionAuthoring.MainPin(phase, side);
                            var cross = DocksideMotionAuthoring.Crosshead(phase, side);
                            var coupling = rods.Find("Coupling rod"); var main = rods.Find("Main rod"); var piston = rods.Find("Piston rod");
                            Require(Mathf.Abs(piston.localScale.x - .035f) < .00001f && Mathf.Abs(piston.localScale.y - .035f) < .00001f, "Partial scale curves must not inflate the piston rod into a box."); assertions++;
                            Vector3 Local(Transform part, Vector3 p) => unit.InverseTransformPoint(part.TransformPoint(p));
                            void Near(Vector3 actual, Vector3 expected, string message)
                            {
                                float error = Vector3.Distance(actual, expected); maxError = Mathf.Max(error, maxError);
                                Require(error < .00035f, message + ": " + error); assertions++;
                            }
                            Near(Local(coupling, new Vector3(0, 0, -.5f)), pin, "Rear coupling pin detached");
                            Near(Local(coupling, new Vector3(0, 0, .5f)), frontPin, "Front coupling pin detached");
                            Near(Local(main, new Vector3(0, 0, -.5f)), mainPin, "Main rod crank detached");
                            Near(Local(main, new Vector3(0, 0, .5f)), cross, "Main rod crosshead detached");
                            Near(unit.InverseTransformPoint(rods.Find("Crosshead").position), cross, "Crosshead axis mismatch");
                            Near(Local(piston, new Vector3(0, 0, -.5f)), DocksideMotionAuthoring.PistonCrosshead(phase, side), "Piston rod crosshead detached");
                            Near(Local(piston, new Vector3(0, 0, .5f)), new Vector3(side * DocksidePlan.PistonAxisX, DocksidePlan.DriverRadius, DocksidePlan.CylinderZ + .05f), "Piston rod cylinder axis mismatch");
                            var axles = unit.Cast<Transform>().Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).OrderBy(t => t.localPosition.z).ToArray();
                            for (int i = 0; i < 2; i++)
                            {
                                var wheel = axles[i].Find("Small driver " + suffix + " (native alias)");
                                var actual = Local(wheel, new Vector3(.98730177f, -DocksidePlan.NativeCrankRadius, 0));
                                var expected = i == 0 ? pin : frontPin;
                                // Native triangle audit separately verifies both bearing X faces.
                                Require(Vector2.Distance(new Vector2(actual.y, actual.z), new Vector2(expected.y, expected.z)) < .001f, "Native wheel crank and rod disagree (" + reader.portId + ", " + suffix + ", sample " + sample + ", crank " + crank + ", rotation " + wheel.localEulerAngles + ", actual " + actual + ", expected " + expected + ")");
                            }
                            var cylinder = unit.Find("Franklin fit " + suffix + "/cylinder");
                            Near(unit.InverseTransformPoint(cylinder.position), new Vector3(side * DocksidePlan.PistonAxisX, DocksidePlan.DriverRadius, DocksidePlan.CylinderZ), "Cylinder axis is not on the piston rod");
                            float gearZ = unit.InverseTransformPoint(unit.Find("Franklin fit " + suffix + "/transmission_box").position).z;
                            Require(Mathf.Abs(gearZ + DocksidePlan.AxlePitch / 2) < .001f, "Franklin gearbox must sit over the rear driver datum."); assertions++;
                        }
                        foreach (var renderer in unit.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            if (renderer.GetComponent<MeshFilter>()?.sharedMesh == null) continue;
                            var bounds = renderer.bounds;
                            sweptX = Mathf.Max(sweptX, Mathf.Abs(clone.transform.InverseTransformPoint(bounds.min).x), Mathf.Abs(clone.transform.InverseTransformPoint(bounds.max).x));
                        }
                    }
                }
            // Prove loop closure and absolute seeking/reverse without relying on
            // accumulated animator speed or a particular sample order.
            foreach (var reader in clone.GetComponentsInChildren<AnimatorPortReaderProxy>(true))
            {
                var clip = reader.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
                var all = reader.GetComponentsInChildren<Transform>(true);
                clip.SampleAnimation(reader.gameObject, 0);
                var positions = all.Select(t => t.localPosition).ToArray(); var rotations = all.Select(t => t.localRotation).ToArray();
                foreach (float t in new[] { .9f, .2f, .75f, .1f, 1f }) clip.SampleAnimation(reader.gameObject, t);
                for (int i = 0; i < all.Length; i++)
                {
                    Require(Vector3.Distance(all[i].localPosition, positions[i]) < .0001f && Quaternion.Angle(all[i].localRotation, rotations[i]) < .1f, "Animation loop/absolute seek drift: " + all[i].name); assertions++;
                }
            }
            int seekChecks = SteamMotionSeekAudit.Validate(clone, true, DocksideMotionAuthoring.Crosshead); assertions += seekChecks;
            SteamBogiePreview.Render(clone, output, "dockside", DocksidePlan.DriverRadius, "Small driver ");
            DocksideWheelComparison.Render(clone, output);
            File.WriteAllText(Path.Combine(output, "motion-validation.json"), "{\"passed\":true,\"reopenedBundle\":true,\"poseAssertions\":" + assertions + ",\"seekAndIsolationAssertions\":" + seekChecks + ",\"independentNativeUnitCranks\":true,\"wrappedReverseSeekAndLoopSeam\":true,\"additionalWheelPhaseOffset\":0,\"modeledVariableCutoffLinkage\":false,\"sampledTurnsPerYaw\":129,\"yawDegrees\":[-12,0,12],\"maxEndpointErrorMetres\":" + Number(maxError) + ",\"sweptHalfWidthMetres\":" + Number(sweptX) + ",\"quarteringDegrees\":90,\"unitOffsetDegrees\":45,\"nativeCrankPortDriven\":true,\"allFiveFranklinShaftsMove\":true,\"loopAndReverseSeek\":true,\"gameRuntimeValidated\":false}\n");
        }
        finally { Object.DestroyImmediate(clone); }
    }
    private static void Preview(GameObject clone, string output)
    {
        // Local kinematic preview only. Native aliases remain unresolved in the
        // SDK; grey wheel discs stand in for the native wheels, no game meshes.
        var material = new Material(Shader.Find("Standard")) { color = new Color(.28f, .30f, .31f) };
        var readers = clone.GetComponentsInChildren<AnimatorPortReaderProxy>(true);
        foreach (var reader in readers)
        {
            reader.transform.localRotation = Quaternion.identity;
            foreach (var wheel in reader.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Small driver ", StringComparison.Ordinal)).ToArray())
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder); disc.transform.SetParent(wheel, false);
                disc.transform.localPosition = new Vector3(.78f, 0, 0); disc.transform.localRotation = Quaternion.Euler(0, 0, 90);
                disc.transform.localScale = new Vector3(DocksidePlan.NativeDriverRadius * 2, .035f, DocksidePlan.NativeDriverRadius * 2);
                disc.GetComponent<Renderer>().sharedMaterial = material;
            }
        }
        foreach (Transform t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
        var cameraObject = new GameObject("Dockside preview camera"); var lightObject = new GameObject("Dockside preview light");
        var previousAmbient = RenderSettings.ambientLight;
        try
        {
            var camera = cameraObject.AddComponent<Camera>(); camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.88f, .91f, .93f);
            camera.orthographic = true; camera.orthographicSize = 2.4f;
            camera.transform.position = new Vector3(7, 2.8f, 4.5f); camera.transform.LookAt(new Vector3(0, .7f, .5f));
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(45, -30, 0);
            RenderSettings.ambientLight = new Color(.55f, .55f, .55f);
            for (int frame = 0; frame < 2; frame++)
            {
                foreach (var reader in readers)
                {
                    float t = frame * .125f + (reader.portId.StartsWith(MeyerSteamAuthoring.Lp + ".", StringComparison.Ordinal) ? .125f : 0);
                    reader.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single().SampleAnimation(reader.gameObject, t);
                }
                var texture = new RenderTexture(1200, 750, 24); camera.targetTexture = texture; camera.Render();
                var before = RenderTexture.active; RenderTexture.active = texture;
                var pixels = new Texture2D(1200, 750, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 1200, 750), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(output, "kinematic-preview-" + frame + ".png"), pixels.EncodeToPNG());
                RenderTexture.active = before; camera.targetTexture = null; Object.DestroyImmediate(pixels); Object.DestroyImmediate(texture);
            }
        }
        finally { RenderSettings.ambientLight = previousAmbient; Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(lightObject); Object.DestroyImmediate(material); }
    }
    private static string Number(float x) => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
