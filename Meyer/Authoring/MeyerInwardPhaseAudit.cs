#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using CCL.Types;
using CCL.Types.Proxies.Ports;
using CCL.Types.Proxies.VFX;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using RearrangedS060;

// Runs inside the existing exact-bundle layout audit. Native readers and the
// candidate presentation methods are used; no game scene or steam model runs.
public static class MeyerInwardPhaseAudit
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_MEYER_INWARD_PHASE_OUTPUT");
        string runtime = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        string bundlePath = Environment.GetEnvironmentVariable("S060_MEYER_AUDIT_BUNDLE");
        Directory.CreateDirectory(output);
        typeof(ExhaustNativeAudit).GetField("managed", Members | BindingFlags.Static).SetValue(null, Managed);
        ResolveEventHandler resolve = (sender, args) => (Assembly)typeof(ExhaustNativeAudit).GetMethod("Resolve", Members | BindingFlags.Static).Invoke(null, new object[] { sender, args });
        AppDomain.CurrentDomain.AssemblyResolve += resolve;
        AssetBundle bundle = null; GameObject clone = null; int checks = 0;
        try
        {
            string runtimeHash = Hash(runtime), bundleHash = Hash(bundlePath);
            bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null) throw new InvalidOperationException("Cannot reopen the exact Meyer donor bundle.");
            var prefab = bundle.LoadAllAssets<CustomCarPack>().Single().Cars.Single().liveries.Single().prefab;
            clone = UnityEngine.Object.Instantiate(prefab);
            var front = clone.transform.Find("BogieF/bogie_car");
            front.localRotation = Quaternion.Euler(0, 180, 0);
            checks = Validate(front, Assembly.LoadFrom(runtime));
            if (Hash(runtime) != runtimeHash || Hash(bundlePath) != bundleHash) throw new InvalidOperationException("Phase candidate changed during audit.");
            File.WriteAllText(Path.Combine(output, "meyer-inward-phase-validation.json"), JsonUtility.ToJson(new Report {
                passed = true, checks = checks, runtimeSha256 = runtimeHash, bundleSha256 = bundleHash,
                actualNativeReaders = true, actualDonorClip = true, absoluteReverseWrappedSeeks = true,
                worldRollingDirection = true, quarteredRodEndpoints = true, nativeCylinderMaskOutlets = true,
                unchangedNativeCrankAndReverser = true, physicalOilCircuitSides = true, gameScene = false }, true));
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(output, "meyer-inward-phase-validation.json"), JsonUtility.ToJson(new Report { passed = false, checks = checks, error = ex.ToString() }, true));
            Debug.LogException(ex); EditorApplication.Exit(1);
        }
        finally
        {
            if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
            if (bundle != null) bundle.Unload(true);
            AppDomain.CurrentDomain.AssemblyResolve -= resolve;
        }
    }
    public static int Validate(Transform front, Assembly production)
    {
        int checks = SteamMeshContact.ValidateAlgorithm();
        var game = Assembly.LoadFrom(Path.Combine(Managed, "Assembly-CSharp.dll"));
        var sim = Assembly.LoadFrom(Path.Combine(Managed, "DV.Simulation.dll"));
        var presentation = production.GetType("RearrangedS060.S060ArticulatedPresentation", true);
        var fixture = new GameObject("Reversed Meyer native phase fixture");
        object nativeReader = null, timing = null, fitting = null; bool initialized = false;
        var mounts = new List<IDisposable>();
        var nativeGeometry = new List<UnityEngine.Object>();
        try
        {
            var effectHomes = front.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Cock front ", StringComparison.Ordinal) || t.name.StartsWith("Cock rear ", StringComparison.Ordinal) || t.name.StartsWith("Water drip ", StringComparison.Ordinal) || t.name.StartsWith("Cock audio ", StringComparison.Ordinal))
                .Select(t => new { part = t, local = front.InverseTransformPoint(t.position) }).ToArray();
            var rodHomes = new[] { "L", "R" }.Select(side => front.Find("[Meyer rods " + side + "]/Main rod").GetComponent<MeshFilter>()).Select(f => new { filter = f, source = f.sharedMesh }).ToArray();
            var fitType = production.GetType("RearrangedS060.S060MeyerFrontMotion", true);
            fitting = Call(fitType, "Bind", front);
            var applyFit = fitType.GetMethod("ApplyPose", Members);
            Require(effectHomes.Length >= 8, "Both moved cylinders retain their native jet/drip/hiss holders", ref checks);
            foreach (var holder in effectHomes) Require(Vector3.Distance(front.InverseTransformPoint(holder.part.position), holder.local + Vector3.forward * S060MeyerLayout.FrontCylinderShift) < .00001f, "Existing drain/hiss holder follows the complete cylinder movement", ref checks);
            var casting = NativeGeometry(front, nativeGeometry);
            var rear = front.parent.parent.Find("BogieR/bogie_car");
            var materialSource = new GameObject("s060_body"); materialSource.transform.SetParent(fixture.transform, false);
            materialSource.AddComponent<MeshRenderer>().sharedMaterial = front.GetComponentsInChildren<MeshRenderer>(true).First(r => r.sharedMaterial != null).sharedMaterial;
            SteamNativePreview.RodSources(fixture.transform, nativeGeometry);
            var wheelHomes = new[] { front, rear }.SelectMany(f => f.Find("[axle] rear").GetComponentsInChildren<MeshFilter>(true)).ToDictionary(f => f, f => f.sharedMesh);
            var mountHomes = new[] { front, rear }.SelectMany(f => f.Find("[Powered bogie frame]").Cast<Transform>()).Where(t => t.name.StartsWith("Leaf spring ", StringComparison.Ordinal) || t.name.StartsWith("Visible upper frame edge ", StringComparison.Ordinal)).ToDictionary(t => t, t => t.localPosition);
            var fittedRodHomes = new[] { front, rear }.SelectMany(f => f.GetComponentsInChildren<MeshFilter>(true)).Where(f => f.name == "Main rod" || f.name == "Coupling rod").ToDictionary(f => f, f => f.sharedMesh);
            var guideHomes = new[] { front, rear }.SelectMany(f => f.GetComponentsInChildren<Transform>(true)).Where(t => t.name.StartsWith("Crosshead guide ", StringComparison.Ordinal)).Select(t => new { part = t, p = t.localPosition, s = t.localScale }).ToArray();
            var mountType = production.GetType("RearrangedS060.S060MeyerGearMounts", true);
            foreach (var frame in new[] { front, rear })
            {
                mounts.Add((IDisposable)mountType.GetMethod("Bind", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { frame, fixture.transform }));
                checks += CheckGearMounts(frame, true);
            }
            foreach (var wheel in wheelHomes) checks += CheckMainJournal(wheel.Value, wheel.Key.sharedMesh);
            var animatorType = game.GetType("DV.Simulation.Ports.AnimatorPortReader", true);
            var clock = fixture.AddComponent(animatorType);
            animatorType.GetField("updateType").SetValue(clock, Enum.Parse(animatorType.GetField("updateType").FieldType, "SET_NORMALIZED_TIME"));
            Call(presentation, "ReverseMeyerMotion", clock);
            float multiplier = (float)animatorType.GetField("valueMultiplier").GetValue(clock);
            float offset = (float)animatorType.GetField("valueOffset").GetValue(clock);
            Require(multiplier == -1 && offset == .75f, "Candidate uses the native absolute reader's -1/.75 phase mapping", ref checks);
            var clip = front.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
            var car = front.parent.parent;
            var cupSide = production.GetType("RearrangedS060.S060OilCups", true).GetMethod("PhysicalLeft", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var frame in new[] { front, car.Find("BogieR/bogie_car") })
            {
                var original = frame.localRotation;
                foreach (float yaw in new[] { -6f, 0f, 6f })
                {
                    frame.localRotation = Quaternion.Euler(0, (frame == front ? 180 : 0) + yaw, 0);
                    foreach (int side in new[] { -1, 1 }) foreach (bool leading in new[] { false, true })
                    {
                        var wheel = frame.Find((leading ? "[axle] front" : "[axle] rear") + "/S060 driver " + (side < 0 ? "L" : "R") + " (native alias)");
                        float rodX = side * (leading ? RearrangedS060.S060OilCupGeometry.CouplingRodPlaneX : RearrangedS060.S060OilCupGeometry.MainRodPlaneX);
                        bool mapped = (bool)cupSide.Invoke(null, new object[] { car, frame, wheel, rodX });
                        Require(mapped == (frame == front ? side > 0 : side < 0),
                            "Candidate oil-cup circuits retain physical car side on both articulated engines", ref checks);
                    }
                }
                frame.localRotation = original;
            }
            var topology = production.GetType("RearrangedS060.S060OilingTopology", true);
            var arrangement = production.GetType("RearrangedS060.S060Arrangement", true);
            foreach (bool left in new[] { true, false })
                Require(((int[])topology.GetMethod("SideCircuits", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,
                    new[] { Enum.Parse(arrangement, "Meyer"), (object)left })).SequenceEqual(left ? new[] { 2, 0, 1, 6 } : new[] { 5, 3, 4, 9 }),
                    "Meyer oil circuit IDs and ordering remain unchanged on both physical sides", ref checks);
            foreach (float nativePhase in new[] { -2.125f, -.00001f, 0f, .00001f, .125f, .25f, .375f, .5f, .625f, .75f, .875f, .99999f, 1f, 3.125f })
            {
                float q = Mathf.Repeat(nativePhase * multiplier + offset, 1);
                clip.SampleAnimation(front.gameObject, q * clip.length);
                applyFit.Invoke(fitting, null);
                var rearClip = rear.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
                rearClip.SampleAnimation(rear.gameObject, Mathf.Repeat(nativePhase, 1) * rearClip.length);
                foreach (var frame in new[] { front, rear }) checks += CheckGearMounts(frame, false);
                foreach (int side in new[] { -1, 1 })
                {
                    string suffix = side < 0 ? "L" : "R";
                    var actual = front.Find("[Meyer rods " + suffix + "]/Crosshead").localPosition;
                    // Inward local L is native/physical R; inward local R is L.
                    int nativeSide = -side;
                    var expected = FittedCrosshead(nativePhase + (nativeSide < 0 ? .25f : 0), side);
                    Require(Mathf.Abs(actual.z - expected.z) < .00035f,
                        "Actual reversed clip piston retains its original native cylinder phase: " + suffix, ref checks);
                    var wheel = front.Find("[axle] rear/S060 driver " + suffix + " (native alias)");
                    var pin = front.InverseTransformPoint(wheel.TransformPoint(new Vector3(.98730177f, -MeyerPlan.CrankRadius, 0)));
                    var expectedPin = MeyerMotionAuthoring.Pin(q + (side < 0 ? .25f : 0), side, false);
                    Require(Vector2.Distance(new Vector2(pin.y, pin.z), new Vector2(expectedPin.y, expectedPin.z)) < .001f,
                        "Native wheel and rod remain quartered after reversed/wrapped seeks", ref checks);
                    var rods = front.Find("[Meyer rods " + suffix + "]");
                    var rearPin = MeyerMotionAuthoring.Pin(q + (side < 0 ? .25f : 0), side, false);
                    var frontPin = MeyerMotionAuthoring.Pin(q + (side < 0 ? .25f : 0), side, true);
                    var mainPin = MeyerMotionAuthoring.MainPin(q + (side < 0 ? .25f : 0), side);
                    var crosshead = FittedCrosshead(q + (side < 0 ? .25f : 0), side);
                    NearWorld(rods.Find("Coupling rod"), new Vector3(0, 0, -.5f), rearPin, front, ref checks);
                    NearWorld(rods.Find("Coupling rod"), new Vector3(0, 0, .5f), frontPin, front, ref checks);
                    NearWorld(rods.Find("Main rod"), new Vector3(0, 0, -.5f), mainPin, front, ref checks);
                    NearWorld(rods.Find("Main rod"), new Vector3(0, 0, .5f), crosshead, front, ref checks);
                    var piston = crosshead; piston.x = side * S060OilCupGeometry.PistonAxisX;
                    NearWorld(rods.Find("Piston rod"), new Vector3(0, 0, -.5f), piston, front, ref checks);
                    NearWorld(rods.Find("Piston rod"), new Vector3(0, 0, .5f), new Vector3(side * 1.075f, (float)MeyerPlan.DriverRadius, S060MeyerLayout.FrontCylinderZ + .05f), front, ref checks);
                    float guideRear = -.14f + S060MeyerLayout.FrontCylinderShift, guideFront = .76f + S060MeyerLayout.FrontCylinderShift;
                    Require(crosshead.z - .065f >= guideRear && crosshead.z + .065f <= guideFront, "Moved front crosshead remains within its connected guides", ref checks);
                    Require(crosshead.z + .065f < .780774f + S060MeyerLayout.FrontCylinderShift - .025f, "Full native front gland clears crosshead at the extended main rod's forward limit", ref checks);
                    foreach (var wheelPart in front.Cast<Transform>().Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).Select(a => a.Cast<Transform>().Single(t => t.name.Contains("driver " + suffix))))
                    {
                        var wheelFilter = wheelPart.GetComponent<MeshFilter>();
                        var cylinderRegion = UsedBounds(casting, front);
                        Require(!new SteamMeshContact.Shell(wheelFilter, front, cylinderRegion).Intersects(new SteamMeshContact.Shell(casting, front, cylinderRegion)), "Full native cylinder clears both actual driver meshes", ref checks);
                        foreach (var part in rods.GetComponentsInChildren<MeshFilter>().Where(f => f.name == "Main rod" || f.name == "Piston rod" || f.name == "Crosshead" || f.name.StartsWith("Crosshead guide", StringComparison.Ordinal) || f.name.StartsWith("Guide ", StringComparison.Ordinal)))
                        {
                            var region = UsedBounds(part, front); var wheelShell = new SteamMeshContact.Shell(wheelFilter, front, region);
                            Require(!wheelShell.Intersects(new SteamMeshContact.Shell(part, front, region)), "Fitted front wheel/gear surface clearance: " + part.name, ref checks);
                        }
                    }
                }
            }
            // A plain negative playback rate misses the left/right exchange.
            clip.SampleAnimation(front.gameObject, 0);
            float naive = front.Find("[Meyer rods L]/Crosshead").localPosition.z;
            Require(Mathf.Abs(naive - MeyerMotionAuthoring.Crosshead(0, -1).z) > .1f,
                "Negative-rate-only baseline reproduces the native cylinder phase mismatch", ref checks);
            // Compare the sampled original donor's world rotation directly;
            // do not assume the native forward crank's world-axis sign. A
            // 1/64 turn also avoids noisy axis extraction near identity.
            var rollingWheel = front.Find("[axle] rear/S060 driver R (native alias)");
            var inwardRotation = front.localRotation;
            foreach (float direction in new[] { 1f, -1f })
            {
                const float step = 1f / 64;
                front.localRotation = Quaternion.Inverse(Quaternion.Euler(0, 180, 0)) * inwardRotation;
                clip.SampleAnimation(front.gameObject, .123f * clip.length);
                var before = rollingWheel.rotation;
                clip.SampleAnimation(front.gameObject, (.123f + step * direction) * clip.length);
                var nativeDelta = rollingWheel.rotation * Quaternion.Inverse(before);
                front.localRotation = inwardRotation;
                clip.SampleAnimation(front.gameObject, Mathf.Repeat(.123f * multiplier + offset, 1) * clip.length);
                before = rollingWheel.rotation;
                clip.SampleAnimation(front.gameObject, Mathf.Repeat((.123f + step * direction) * multiplier + offset, 1) * clip.length);
                var delta = rollingWheel.rotation * Quaternion.Inverse(before);
                float nativeDegrees = Quaternion.Angle(Quaternion.identity, nativeDelta);
                float candidateDegrees = Quaternion.Angle(Quaternion.identity, delta);
                var a = new Vector4(nativeDelta.x, nativeDelta.y, nativeDelta.z, nativeDelta.w);
                var b = new Vector4(delta.x, delta.y, delta.z, delta.w);
                float error = Mathf.Min(Vector4.Distance(a, b), Vector4.Distance(a, -b));
                Require(Mathf.Abs(nativeDegrees - 360 * step) < .01f && Mathf.Abs(candidateDegrees - 360 * step) < .01f && error < .00001f,
                    "Actual inward driver must retain the original donor's forward/reverse world delta: direction=" + direction +
                    ", nativeDegrees=" + nativeDegrees + ", inwardDegrees=" + candidateDegrees + ", quaternionComponentError=" + error +
                    ", nativeDelta=" + nativeDelta + ", inwardDelta=" + delta, ref checks);
            }

            var donor = front.GetComponentsInChildren<CylinderCockParticlePortReaderProxy>(true).Single();
            var readerType = game.GetType("DV.Simulation.Controllers.CylinderCockParticlePortReader", true);
            nativeReader = fixture.AddComponent(readerType);
            var setupType = readerType.GetNestedType("CylinderSetup"); var setups = Array.CreateInstance(setupType, 2);
            var ps = new ParticleSystem[2, 2];
            for (int cylinder = 0; cylinder < 2; cylinder++)
            {
                var setup = Activator.CreateInstance(setupType);
                for (int end = 0; end < 2; end++)
                {
                    string name = end == 0 ? "front" : "rear";
                    var holder = new GameObject("physical local " + cylinder + " " + name); holder.transform.SetParent(fixture.transform, false);
                    ps[cylinder, end] = holder.AddComponent<ParticleSystem>();
                    var main = ps[cylinder, end].main; main.playOnAwake = false; main.startLifetime = .1f;
                    ps[cylinder, end].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    setupType.GetField(name + "ParticlesParent").SetValue(setup, holder);
                    setupType.GetField(name + "ActivityCurve").SetValue(setup, end == 0 ? donor.cylinderSetups[cylinder].frontActivityCurve : donor.cylinderSetups[cylinder].rearActivityCurve);
                }
                setups.SetValue(setup, cylinder);
            }
            readerType.GetField("cylinderSetups").SetValue(nativeReader, setups);
            foreach (string name in new[] { "startSpeedMultiplierMin", "startSpeedMultiplierMax", "startSizeMultiplierMin", "startSizeMultiplierMax", "emissionRateMultiplierMin", "emissionRateMultiplierMax" })
                readerType.GetField(name).SetValue(nativeReader, 1f);
            Call(presentation, "ReverseMeyerOutlets", nativeReader);
            for (int cylinder = 0; cylinder < 2; cylinder++)
                Require(ReferenceEquals(setupType.GetField("frontActivityCurve").GetValue(setups.GetValue(cylinder)), donor.cylinderSetups[cylinder].frontActivityCurve),
                    "Reversed holder mapping retains each native cylinder's original activity curve", ref checks);
            var portType = sim.GetType("LocoSim.Implementations.Port", true);
            var definition = sim.GetType("LocoSim.Definitions.PortDefinition", true);
            var typeEnum = sim.GetType("LocoSim.Definitions.PortType", true); var valueEnum = sim.GetType("LocoSim.Definitions.PortValueType", true);
            var ports = (IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(string), portType));
            foreach (string name in new[] { "crankRotation", "reverser", "cylindersInletValveOpen", "cylinderCockFlowNormalized", "forwardSpeed" })
            {
                var def = Activator.CreateInstance(definition, new[] { Enum.Parse(typeEnum, "READONLY_OUT"), Enum.Parse(valueEnum, "GENERIC"), (object)name });
                ports.Add("inward." + name, Activator.CreateInstance(portType, new[] { "inward", def, (object)0f }));
                readerType.GetField(name + "PortId").SetValue(nativeReader, "inward." + name);
            }
            var flowType = sim.GetType("LocoSim.Implementations.SimulationFlow", true); var flow = FormatterServices.GetUninitializedObject(flowType);
            flowType.GetField("fullPortIdToPort", Members).SetValue(flow, Activator.CreateInstance(typeof(System.Collections.ObjectModel.ReadOnlyDictionary<,>).MakeGenericType(typeof(string), portType), ports));
            Action<string, float> set = (name, value) => portType.GetProperty("Value").SetValue(ports["inward." + name], value, null);
            readerType.GetMethod("Init").Invoke(nativeReader, new[] { flow });
            initialized = true;
            timing = Call(production.GetType("RearrangedS060.S060CockTiming", true), "Bind", nativeReader);
            set("cylinderCockFlowNormalized", 1);
            foreach (float direction in new[] { 1f, -1f }) foreach (int active in new[] { 0, 1 }) foreach (float p in new[] { .125f, .375f, .625f, .875f })
            {
                set("reverser", direction); set("crankRotation", p); set("cylindersInletValveOpen", 1 << active);
                Require((float)portType.GetProperty("Value").GetValue(ports["inward.crankRotation"], null) == p &&
                    (float)portType.GetProperty("Value").GetValue(ports["inward.reverser"], null) == direction,
                    "Presentation retains the exact native crank and reverser port values", ref checks);
                for (int physical = 0; physical < 2; physical++) for (int end = 0; end < 2; end++)
                {
                    var source = donor.cylinderSetups[active];
                    bool frontCurve = (end == 0) == (direction > 0);
                    bool expected = physical == 1 - active && (frontCurve ? source.frontActivityCurve : source.rearActivityCurve).Evaluate(p) > 0;
                    Require(ps[physical, end].isEmitting == expected, "Actual native inlet masks/reverser select the matching inward cylinder outlet", ref checks);
                }
            }
            var audioType = game.GetType("DV.ModularAudioCar.CylinderCockLayeredPortReader", true);
            var hiss = fixture.AddComponent(audioType); var field = audioType.GetField("cylCockAudio");
            var holders = Array.CreateInstance(field.FieldType.GetElementType(), 2);
            for (int i = 0; i < 2; i++) { var holder = new GameObject("physical hiss " + i); holder.transform.SetParent(fixture.transform, false); holders.SetValue(holder.AddComponent(field.FieldType.GetElementType()), i); }
            object first = holders.GetValue(0), second = holders.GetValue(1); field.SetValue(hiss, holders);
            Call(presentation, "ReverseMeyerAudio", hiss);
            Require(ReferenceEquals(holders.GetValue(0), second) && ReferenceEquals(holders.GetValue(1), first), "Native per-cylinder hiss follows the same physical side mapping as jets", ref checks);
            foreach (var mount in mounts) mount.Dispose(); mounts.Clear();
            foreach (var home in fittedRodHomes) Require(home.Key.sharedMesh == home.Value, "Both engines restore each exact main and coupling mesh", ref checks);
            foreach (var home in guideHomes) Require(home.part.localPosition == home.p && home.part.localScale == home.s, "Both engines restore exact guide positions and widths", ref checks);
            foreach (var home in wheelHomes) Require(home.Key.sharedMesh == home.Value, "Both engines restore their exact native wheel mesh references", ref checks);
            foreach (var home in mountHomes) Require(home.Key.localPosition == home.Value, "Both engines restore exact spring and frame-edge positions", ref checks);
            (fitting as IDisposable)?.Dispose(); fitting = null;
            foreach (var home in rodHomes) Require(home.filter.sharedMesh == home.source, "Front fitted rod restores its exact donor mesh", ref checks);
            return checks;
        }
        finally
        {
            (timing as IDisposable)?.Dispose(); foreach (var mount in mounts) mount.Dispose(); (fitting as IDisposable)?.Dispose();
            if (initialized) nativeReader.GetType().GetMethod("Deinit").Invoke(nativeReader, null);
            UnityEngine.Object.DestroyImmediate(fixture);
            foreach (var item in nativeGeometry) if (item != null) UnityEngine.Object.DestroyImmediate(item);
        }
    }
    private static Vector3 FittedCrosshead(float phase, int side)
    {
        var pin = MeyerMotionAuthoring.MainPin(phase, side); float dy = pin.y - (float)MeyerPlan.DriverRadius;
        return new Vector3(pin.x, (float)MeyerPlan.DriverRadius, pin.z + Mathf.Sqrt(S060MeyerLayout.FrontMainRodLength * S060MeyerLayout.FrontMainRodLength - dy * dy));
    }
    public static int CheckMainJournal(Mesh original, Mesh fitted)
    {
        int checks = 0; var vertices = original.vertices; var sourceTriangles = original.triangles;
        var ids = S060MeyerClearance.Islands(vertices, new[] { sourceTriangles }, out var bounds);
        var referenced = sourceTriangles.Distinct().ToArray();
        var counts = referenced.GroupBy(i => ids[i]).ToDictionary(g => g.Key, g => g.Count());
        var removed = bounds.Select((b, id) => new { b, id }).Where(p => counts.TryGetValue(p.id, out var n) &&
            ((n == 122 && p.b.min.x > 1.08f && p.b.max.x < 1.13f && p.b.min.y < -.46f && p.b.max.y > 0) ||
             (n == 43 && p.b.min.x > 1.06f && p.b.max.x > 1.17f && p.b.max.y < 0))).Select(p => p.id).ToArray();
        Require(removed.Length == 2, "Actual native unused return lever and offset pin fixtures are present", ref checks);
        var used = new HashSet<int>(fitted.triangles);
        Require(used.All(i => !removed.Contains(ids[i])), "Both unused return-crank islands are absent from referenced wheel triangles", ref checks);
        var shaft = bounds.Select((b, id) => new { b, id }).Single(p => counts.TryGetValue(p.id, out var n) && n == 51 && p.b.min.x > .90f && p.b.max.x > 1.13f && p.b.max.x < 1.14f && p.b.max.y < -.27f).id;
        Require(referenced.Where(i => ids[i] == shaft).All(used.Contains), "Complete long main journal remains referenced", ref checks);
        int caps = 0; var triangles = fitted.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
            if (Enumerable.Range(0, 3).All(k => ids[triangles[i + k]] == shaft && Mathf.Abs(vertices[triangles[i + k]].x - 1.13335454f) < .00001f)) caps++;
        Require(caps == 16, "Native long main journal retains its existing closed outer cap", ref checks);
        Require(fitted.vertices.SequenceEqual(vertices), "Wheel fitting leaves all native vertex positions unchanged", ref checks);
        return checks;
    }
    public static int CheckGearMounts(Transform frame, bool connections)
    {
        int checks = 0; var mount = frame.Find("[Meyer motion brackets and spring mounts]").GetComponent<MeshFilter>();
        var wheels = frame.Cast<Transform>().Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).SelectMany(t => t.GetComponentsInChildren<MeshFilter>(true)).ToArray();
        Require(wheels.Length == 4, "Both actual native wheel sides on both axles are present", ref checks);
        foreach (var wheel in wheels)
        {
            var region = UsedBounds(wheel, frame);
            Require(!new SteamMeshContact.Shell(mount, frame, region).Intersects(new SteamMeshContact.Shell(wheel, frame, region), out var hit), "Actual motion/spring mounts clear moving native wheel triangles: " + hit, ref checks);
        }
        foreach (string suffix in new[] { "L", "R" })
        {
            var rods = frame.Find("[Meyer rods " + suffix + "]");
            // Connection checks also run on unsampled donor defaults. Evaluate
            // rod/pin contact only after the real clip and front fitter pose them.
            if (!connections) foreach (string name in new[] { "Main rod", "Coupling rod" })
            {
                var rod = rods.Find(name).GetComponent<MeshFilter>(); var region = UsedBounds(rod, frame);
                foreach (var wheel in wheels.Where(w => w.name.Contains("driver " + suffix)))
                {
                    var rodShell = new SteamMeshContact.Shell(rod, frame, region); var wheelShell = new SteamMeshContact.Shell(wheel, frame, region);
                    bool contact = rodShell.Intersects(wheelShell, out var pinHit);
                    var end = frame.InverseTransformPoint(rod.transform.TransformPoint(new Vector3(0, 0, -.5f)));
                    var actual = frame.InverseTransformPoint(S060OilCupGeometry.Pin(frame, wheel.transform, (suffix == "L" ? -1 : 1) * S060OilCupGeometry.MainRodPlaneX));
                    Require(!contact, "Sampled native rod/pin clearance on " + frame.parent.name + ", rodPin=" + end.ToString("F9") + ", wheelPin=" + actual.ToString("F9") + ", reverseContact=" + wheelShell.Intersects(rodShell) + ": " + pinHit, ref checks);
                }
            }
            foreach (string name in new[] { "Main rod", "Coupling rod", "Crosshead" })
            {
                var part = rods.Find(name).GetComponent<MeshFilter>(); var region = UsedBounds(part, frame);
                Require(!new SteamMeshContact.Shell(mount, frame, region).Intersects(new SteamMeshContact.Shell(part, frame, region), out var hit), "Mounts clear actual sampled " + name + " on both units: " + hit, ref checks);
                if (name == "Main rod") foreach (var guide in rods.Cast<Transform>().Where(t => t.name.StartsWith("Crosshead guide ", StringComparison.Ordinal)))
                    Require(!new SteamMeshContact.Shell(part, frame).Intersects(new SteamMeshContact.Shell(guide.GetComponent<MeshFilter>(), frame), out var guideHit), "Native main-rod eye clears its outer guides over the sampled cycle: " + guideHit, ref checks);
            }
            if (connections) foreach (var tie in rods.Cast<Transform>().Where(t => t.name == "Guide lower tie"))
            { var filter = tie.GetComponent<MeshFilter>(); var region = UsedBounds(filter, frame); Require(new SteamMeshContact.Shell(mount, frame, region).Intersects(new SteamMeshContact.Shell(filter, frame, region)), "Outer return bracket physically meets the actual guide tie", ref checks); }
        }
        var structure = frame.Find("[Powered bogie frame]");
        float wheelBack = wheels.Min(f => { var v = f.sharedMesh.vertices; return f.sharedMesh.triangles.Distinct().Where(i => new Vector2(v[i].y, v[i].z).magnitude > .25f).Min(i => v[i].x); });
        foreach (var leaf in structure.Cast<Transform>().Where(t => t.name.StartsWith("Leaf spring ", StringComparison.Ordinal)))
        {
            var filter = leaf.GetComponent<MeshFilter>(); var region = UsedBounds(filter, frame);
            float outboard = Mathf.Max(Mathf.Abs(region.min.x), Mathf.Abs(region.max.x));
            Require(wheelBack - outboard > .015f, "Moved spring pack clears the complete rotating native rim/web envelope", ref checks);
            if (connections && leaf.name.EndsWith(" 0", StringComparison.Ordinal)) Require(new SteamMeshContact.Shell(mount, frame, region).Intersects(new SteamMeshContact.Shell(filter, frame, region)), "Spring bottom leaf sits on its actual mounting seat", ref checks);
        }
        if (connections) foreach (var rail in structure.Cast<Transform>().Where(t => t.name.StartsWith("Main frame rail ", StringComparison.Ordinal)))
        { var filter = rail.GetComponent<MeshFilter>(); var region = UsedBounds(filter, frame); Require(new SteamMeshContact.Shell(mount, frame, region).Intersects(new SteamMeshContact.Shell(filter, frame, region)), "Motion bracket pedestal meets existing frame rail", ref checks); }
        return checks;
    }
    [Serializable] private sealed class NativeData { public NativeEntry[] meshes = Array.Empty<NativeEntry>(); }
    [Serializable] private sealed class NativeEntry { public string name = ""; public Vector3[] vertices = Array.Empty<Vector3>(); public int[] triangles = Array.Empty<int>(); }
    private static MeshFilter NativeGeometry(Transform front, List<UnityEngine.Object> owned)
    {
        var data = JsonUtility.FromJson<NativeData>(File.ReadAllText(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")));
        foreach (string station in new[] { "rear", "front" })
        {
            var source = data.meshes.Single(m => m.name == (station == "rear" ? "s060_Wheels_01" : "s060_Wheels_02"));
            var mesh = new Mesh { name = source.name + " actual native fit fixture" }; owned.Add(mesh); mesh.vertices = source.vertices; mesh.triangles = source.triangles; mesh.RecalculateBounds();
            foreach (var frame in new[] { front, front.parent.parent.Find("BogieR/bogie_car") })
                foreach (string side in new[] { "L", "R" }) frame.Find("[axle] " + station + "/S060 driver " + side + " (native alias)").GetComponent<MeshFilter>().sharedMesh = mesh;
        }
        var body = data.meshes.Single(m => m.name == "s060_body");
        var selected = Enumerable.Range(0, body.triangles.Length / 3).Where(i => Enumerable.Range(0, 3).All(k => SteamNativePreview.NativeCylinder(body.vertices[body.triangles[3 * i + k]], -1) || SteamNativePreview.NativeCylinder(body.vertices[body.triangles[3 * i + k]], 1))).SelectMany(i => new[] { body.triangles[3 * i], body.triangles[3 * i + 1], body.triangles[3 * i + 2] }).ToArray();
        var used = selected.Distinct().ToArray(); var map = used.Select((index, local) => new { index, local }).ToDictionary(p => p.index, p => p.local);
        var casting = new Mesh { name = "Actual full native front casting fixture" }; owned.Add(casting); casting.vertices = used.Select(i => body.vertices[i]).ToArray(); casting.triangles = selected.Select(i => map[i]).ToArray(); casting.RecalculateBounds();
        var go = new GameObject("Native moved front casting fixture"); owned.Add(go); go.transform.SetParent(front, false); go.transform.localPosition = Vector3.forward * (S060MeyerLayout.FrontCylinderZ - S060PipeGeometry.NativeCylinderCentreZ);
        var filter = go.AddComponent<MeshFilter>(); filter.sharedMesh = casting; return filter;
    }
    private static Bounds UsedBounds(MeshFilter filter, Transform space)
    {
        var points = filter.sharedMesh.vertices; var used = filter.sharedMesh.triangles.Distinct().ToArray();
        var bounds = new Bounds(space.InverseTransformPoint(filter.transform.TransformPoint(points[used[0]])), Vector3.zero);
        foreach (int index in used) bounds.Encapsulate(space.InverseTransformPoint(filter.transform.TransformPoint(points[index]))); bounds.Expand(.00001f); return bounds;
    }
    private static object Call(Type type, string name, object value) => type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { value });
    private static void NearWorld(Transform part, Vector3 endpoint, Vector3 expected, Transform frame, ref int checks) =>
        Require(Vector3.Distance(part.TransformPoint(endpoint), frame.TransformPoint(expected)) < .00035f,
            "Reversed sampled rod endpoint remains connected in world space: " + part.name, ref checks);
    private static void Require(bool condition, string message, ref int checks) { if (!condition) throw new InvalidOperationException(message); checks++; }
    private static string Managed => Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED") ?? @"B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed";
    private static string Hash(string path) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
    [Serializable] private sealed class Report
    {
        public bool passed, actualNativeReaders, actualDonorClip, absoluteReverseWrappedSeeks, worldRollingDirection,
            quarteredRodEndpoints, nativeCylinderMaskOutlets, unchangedNativeCrankAndReverser, physicalOilCircuitSides, gameScene;
        public int checks; public string runtimeSha256, bundleSha256, error;
    }
}
#endif
