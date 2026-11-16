using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Exact native hierarchy/meshes, shipping rod swap and shipping cup mounts.
// No simulation, streaming or game-session acceptance is claimed.
public static class ConventionalOilRodAudit
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly Type Fixture = typeof(ConventionalNativeAudit);
    static Assembly runtime, game;
    static string output;
    static int checks;
    static bool baseline;
    static readonly List<string> observations = new List<string>();
    static object F(string method, params object[] args) => Fixture.GetMethod(method, All).Invoke(null, args);
    static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, All).Invoke(owner, args);
    static void Field(object owner, string name, object value) => owner.GetType().GetField(name, All).SetValue(owner, value);
    static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); checks++; }
    static object Read(string path) => JsonUtility.FromJson(File.ReadAllText(path), Fixture.GetNestedType("Data", All));
    static object[] Items(object data, string name) => ((IEnumerable)data.GetType().GetField(name).GetValue(data)).Cast<object>().ToArray();
    static string Name(object entry) => (string)entry.GetType().GetField("name").GetValue(entry);
    public static void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("S060_OIL_ROD_OUTPUT"); Directory.CreateDirectory(output);
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => (Assembly)F("Resolve", s, e);
        try
        {
            string dll = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME"); runtime = Assembly.LoadFrom(dll);
            game = Assembly.LoadFrom(Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED"), "Assembly-CSharp.dll"));
            Fixture.GetField("runtime", All).SetValue(null, runtime); Fixture.GetField("output", All).SetValue(null, output);
            var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")), "native-preview-atlas.png")));
            var material = new Material(Shader.Find("Standard")); material.mainTexture = texture; Fixture.GetField("material", All).SetValue(null, material);
            var native = Read(Environment.GetEnvironmentVariable("S060_CONVENTIONAL_NATIVE"));
            var body = Items(Read(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")), "meshes").Single(e => Name(e) == "s060_body");
            var cups = Read(Environment.GetEnvironmentVariable("S060_CONVENTIONAL_OIL_NATIVE"));
            baseline = Environment.GetEnvironmentVariable("S060_OIL_ROD_BASELINE") == "1";
            foreach (string layout in new[] { "stock-060", "short-060", "040", "080", "0100", "062" }) Validate(native, body, cups, layout);
            File.WriteAllLines(Path.Combine(output, "observations.txt"), observations);
            File.WriteAllText(Path.Combine(output, "validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"baseline\":" + baseline.ToString().ToLowerInvariant() + ",\"runtimeSha256\":\"" + F("Hash", dll) + "\",\"layouts\":[\"stock060\",\"short060\",\"040\",\"080\",\"0100\",\"062\"],\"posesPerSide\":24,\"nativeLod0AndLod1\":true,\"gameplayValidated\":false}");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { File.WriteAllText(Path.Combine(output, "error.txt"), e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Validate(object data, object bodyData, object cupData, string layout)
    {
        var root = new GameObject("S060 oil/rod " + layout); root.SetActive(false);
        var car = root.AddComponent(game.GetType("TrainCar", true));
        var body = new GameObject("LocoS060_Body").transform; body.SetParent(root.transform, false);
        foreach (var node in Items(data, "roots")) F("Tree", body, node, data);
        var bodyFilter = ((Transform)F("Draw", body, "s060_body", F("Mesh", bodyData, null))).GetComponent<MeshFilter>();
        var left = body.Find("DrivingMechanism_L"); var right = body.Find("DrivingMechanism_R");
        var parts = F("New", "S060Parts"); F("Set", parts, "Car", car); F("Set", parts, "Body", body); F("Set", parts, "MechanismLeft", left); F("Set", parts, "MechanismRight", right);
        int count = layout == "040" ? 2 : layout == "080" ? 4 : layout == "0100" ? 5 : 3;
        float factor = layout == "0100" ? 1f / 1.424256f : layout == "080" || layout == "short-060" ? .8f : 1f;
        foreach (var side in new[] { left, right })
        {
            side.localScale = new Vector3(side.localScale.x, factor, factor);
            if (count == 2) side.Find("s060_Wheels_02").gameObject.SetActive(false);
            for (int i = 4; i <= count; i++)
            {
                var wheel = Object.Instantiate(side.Find("s060_Wheels_02").gameObject, side).transform;
                wheel.name = "s060_Wheels_0" + i + " (rearranged)";
                wheel.localPosition = new Vector3(wheel.localPosition.x, wheel.localPosition.y, -1.3978f - (i - 3) * 1.5493f);
            }
            if (count >= 4) side.localPosition += Vector3.forward * ((count == 5 ? -2f : -1.3978f) - (-1.3978f - (count - 3) * 1.5493f) * factor);
        }
        var originals = new[] { left, right }.SelectMany(s => s.Find("s060_mech_wheels_connect").GetComponentsInChildren<MeshFilter>(true)).ToDictionary(f => f, f => f.sharedMesh);
        root.SetActive(true);
        IDisposable rods = null;
        var oilType = runtime.GetType("RearrangedS060.S060OilCups", true);
        var oil = root.AddComponent(oilType); Field(oil, "car", car);
        Field(oil, "stemMaterialSource", bodyFilter.GetComponent<MeshRenderer>()); Field(oil, "stemMaterial", bodyFilter.GetComponent<MeshRenderer>().sharedMaterial);
        var external = new GameObject("Local native cup fixture").transform; external.SetParent(root.transform, false);
        foreach (var node in Items(cupData, "roots")) F("Tree", external, node, cupData);
        foreach (var filter in external.GetComponentsInChildren<MeshFilter>(true).Where(f => f.name == "OilCup_LOD0")) filter.gameObject.SetActive(true);
        var consumerType = game.GetType("DV.Util.PositionSyncConsumer", true); var providerType = game.GetType("DV.Util.PositionSyncProvider", true);
        var nativeConsumers = new List<Component>();
        try
        {
            if (count != 3) rods = (IDisposable)runtime.GetType("RearrangedS060.S060CouplingRods", true).GetMethod("Apply", All).Invoke(null, new[] { parts });
            foreach (var filter in originals.Keys)
            {
                if (count == 3) Check(filter.sharedMesh == originals[filter], "Three-axle native rods remain untouched");
                else if (!baseline)
                {
                    Check(filter.sharedMesh.name.Contains("native"), "Fitted conventional rods use native geometry at both LODs");
                    Check(filter.sharedMesh.uv.All(uv => originals[filter].uv.Contains(uv)), "Every fitted rod UV comes from its own native LOD");
                    Check(filter.sharedMesh.vertices.All(p => p.x >= originals[filter].bounds.min.x - .00001f && p.x <= originals[filter].bounds.max.x + .00001f), "Native rod thickness retained");
                }
            }
            var sites = ((IEnumerable)Call(oil, "Sites")).Cast<object>().ToArray();
            foreach (var side in new[] { left, right })
            {
                int offset = side == left ? 0 : 3;
                for (int i = 0; i < 3; i++)
                {
                    var cup = external.Find("ManualOilingPoints/OilingPoint" + (offset + i));
                    var consumer = cup.gameObject.AddComponent(consumerType); var provider = side.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Oiling" + i).gameObject.AddComponent(providerType);
                    Field(consumer, "syncTag", "QA_" + offset + "_" + i); Field(provider, "syncTag", "QA_" + offset + "_" + i); Call(consumer, "SetProviderTransform", provider); Call(consumer, "Sync");
                    nativeConsumers.Add(consumer);
                    if (i == 0 && !baseline) Call(oil, "FitConventionalGuide", consumer, body);
                    if (count == 2 && i == 1) cup.gameObject.SetActive(false);
                }
                var sideSites = sites.Where(s => (bool)s.GetType().GetField("Left", All).GetValue(s) == (side == left)).Where(s => !(bool)s.GetType().GetField("Native", All).GetValue(s)).OrderByDescending(s => Mathf.Abs(((Transform)s.GetType().GetField("Wheel", All).GetValue(s)).localPosition.z + 1.3978f)).ToArray();
                for (int extra = 0; extra < count - 3; extra++)
                {
                    var template = external.Find("ManualOilingPoints/OilingPoint" + (offset + 2));
                    var copy = Object.Instantiate(template.gameObject, external).transform; copy.name = "Extra big end " + offset + " " + extra;
                    var consumer = copy.GetComponent(consumerType); Field(consumer, "syncTag", "EXTRA_" + offset + "_" + extra);
                    Call(oil, "NewMount", sideSites[extra], consumer, "EXTRA_" + offset + "_" + extra);
                }
            }
            Check(external.GetComponentsInChildren(consumerType, false).Length == count * 2, "Exactly two visible service cups per driven axle");
            foreach (var side in new[] { left, right })
            {
                var wheels = side.Cast<Transform>().Where(t => t.name.StartsWith("s060_Wheels_0") && !t.name.Contains("LOD") && t.gameObject.activeSelf).OrderBy(t => t.localPosition.z).ToArray();
                var rotations = wheels.Select(w => w.localRotation).ToArray(); var drive = side.Find("s060_Wheels_01");
                var rod = side.Find("s060_mech_wheels_connect"); var home = rod.localPosition;
                var initial = drive.TransformPoint(new Vector3(.98730177f, -.33535485f, 0));
                for (int phase = 0; phase < 360; phase += 15)
                {
                    for (int i = 0; i < wheels.Length; i++) wheels[i].localRotation = rotations[i] * Quaternion.Euler(phase, 0, 0);
                    rod.localPosition = home + side.InverseTransformVector(drive.TransformPoint(new Vector3(.98730177f, -.33535485f, 0)) - initial);
                    foreach (var consumer in nativeConsumers) if (consumer.gameObject.activeSelf) Call(consumer, "Sync");
                    Call(oil, "LateUpdate");
                    checks += ArticulatedOilCupNativeAudit.CheckMounts(oil, originals.Keys.Concat(new[] { bodyFilter }).ToArray());
                    for (int cupIndex = 0; cupIndex < nativeConsumers.Count; cupIndex++)
                    {
                        var consumer = nativeConsumers[cupIndex]; if (!consumer.gameObject.activeSelf || baseline) continue;
                        var filter = consumer.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "OilCup_LOD0");
                        var foot = (Vector3)runtime.GetType("RearrangedS060.S060OilCupGeometry").GetMethod("NativeThreadFoot", All).Invoke(null, new object[] { filter.sharedMesh.name, filter.sharedMesh.bounds });
                        var sideFrame = cupIndex < 3 ? left : right; int nativeSlot = cupIndex % 3;
                        var support = nativeSlot == 0 ? bodyFilter : sideFrame.Find(nativeSlot == 1 ? "s060_mech_push_rod" : "s060_mech_push_rod_to_connect").GetComponent<MeshFilter>();
                        Check(ArticulatedOilCupNativeAudit.SurfaceDistance(filter.transform.TransformPoint(foot), support) < .012f, "Every retained native cup thread remains seated in its native supporting part");
                    }
                    if (count != 3 && !baseline) foreach (var wheel in wheels)
                    {
                        var pin = rod.InverseTransformPoint(wheel.TransformPoint(new Vector3(.98730177f, -.33535485f, 0)));
                        foreach (var filter in rod.GetComponentsInChildren<MeshFilter>(true))
                            Check(filter.sharedMesh.vertices.Any(v => Mathf.Abs(v.z - pin.z) < .009f && Mathf.Abs(v.y - pin.y) > .09f), "Each fitted native eye follows its crank pin at both LODs");
                    }
                }
                for (int i = 0; i < wheels.Length; i++) wheels[i].localRotation = rotations[i]; rod.localPosition = home;
            }
            foreach (var consumer in nativeConsumers) if (consumer.gameObject.activeSelf) Call(consumer, "Sync"); Call(oil, "LateUpdate");
            foreach (int index in new[] { 0, 3 })
            {
                var cup = external.Find("ManualOilingPoints/OilingPoint" + index);
                var filter = cup.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "OilCup_LOD0");
                var foot = filter.transform.TransformPoint(new Vector3(0, filter.sharedMesh.bounds.min.y, 0));
                // Exact upper guide triangle is horizontal. Both stock rails
                // stay on the body; they are not children of the resized frame.
                float gap = foot.y - body.TransformPoint(new Vector3(0, 1.1024765f, 0)).y;
                observations.Add(layout + " guide " + index + " signed gap to native rail=" + gap.ToString("R"));
                if (!baseline) Check(Mathf.Abs(gap + .006f) < .00003f && foot.z >= 1.11125f && foot.z <= 2.73302f && (index == 0 ? foot.x < -1 : foot.x > 1), "Native guide cup thread seats in the actual body guide rail on its original side");
            }
            F("RenderWide", root, layout + "-cups-rods", new Vector3(7, 1.2f, .2f), new Vector3(0, .8f, .2f), 1.15f);
        }
        finally
        {
            Call(oil, "Clear"); rods?.Dispose(); rods?.Dispose();
            foreach (var pair in originals) Check(pair.Key.sharedMesh == pair.Value && pair.Value != null, "Exact native source rod restored");
            Object.DestroyImmediate(root);
        }
    }
}
