#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Actual native axle discovery and track lookup, with inert native components.
// No live game, traction simulation or saved vehicle is created.
public static class S060SwitchNativeAudit
{
    private const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static int checks;
    private static void Check(bool ok, string why) { checks++; if (!ok) throw new InvalidOperationException(why); }
    private static void Set(object o, string name, object value) => o.GetType().GetField(name, All).SetValue(o, value);
    private static Transform Child(Transform p, string name) { var c = new GameObject(name).transform; c.SetParent(p, false); return c; }
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_SWITCH_OUTPUT"), dll = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        string managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED"); Directory.CreateDirectory(output);
        ResolveEventHandler resolve = (s, a) => {
            string n = new AssemblyName(a.Name).Name;
            var found = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == n); if (found != null) return found;
            foreach (string d in new[] { managed, Path.Combine(managed, "UnityModManager"), Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MODS"), "LocoMeshSplitter") })
                if (File.Exists(Path.Combine(d, n + ".dll"))) return Assembly.LoadFrom(Path.Combine(d, n + ".dll"));
            return null;
        };
        AppDomain.CurrentDomain.AssemblyResolve += resolve;
        var root = new GameObject("Inert S060 switch fixture"); root.SetActive(false);
        object registry = null, previousRegistry = null; FieldInfo singleton = null;
        try
        {
            var runtime = Assembly.LoadFrom(dll); var game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
            var car = root.AddComponent(game.GetType("TrainCar", true));
            Set(car, "carType", Enum.Parse(car.GetType().GetField("carType").FieldType, "LocoS060"));
            var body = Child(root.transform, "LocoS060_Body");
            foreach (string side in new[] { "L", "R" }) for (int i = 1; i <= 3; i++)
            {
                var mech = body.Find("DrivingMechanism_" + side) ?? Child(body, "DrivingMechanism_" + side);
                Child(mech, "s060_Wheels_0" + i);
            }
            var bogieType = game.GetType("Bogie", true);
            var wheels = Assembly.LoadFrom(Path.Combine(managed, "DV.Simulation.dll")).GetType("LocoSim.Implementations.Wheels.PoweredWheel", true);
            var rear = Child(root.transform, "Axle_R"); var front = Child(root.transform, "Axle_F");
            var rb = rear.gameObject.AddComponent(bogieType); var fb = front.gameObject.AddComponent(bogieType);
            var bogies = Array.CreateInstance(bogieType, 2); bogies.SetValue(rb, 0); bogies.SetValue(fb, 1); Set(car, "_bogies", bogies);
            Set(rb, "isFrontBogie", false); Set(fb, "isFrontBogie", true);
            foreach (var b in new[] { rb, fb })
            {
                var t = ((Component)b).transform; var pos = new Vector3(0, 0, b == rb ? -1.4f : .925f);
                Set(b, "startingLocalPosition", pos); Set(b, "startingLocalRotation", Quaternion.identity); t.localPosition = pos;
                var frame = Child(t, "bogie_car");
                foreach (float z in b == rb ? new[] { -1.3978f } : new[] { .1498f, 1.7006f })
                {
                    var axle = Child(frame, "[axle]"); axle.localPosition = new Vector3(0, .712128f, z - pos.z); axle.gameObject.AddComponent(wheels);
                }
            }
            var parts = runtime.GetType("RearrangedS060.S060Parts", true); var resolveParts = parts.GetMethod("Resolve", All);
            foreach (float carYaw in new[] { 0f, 73f }) foreach (float yaw in new[] { -15f, 0f, 15f }) foreach (float displaced in new[] { 0f, -1.3282f, .249f })
            {
                root.transform.SetPositionAndRotation(new Vector3(120, 20, -73), Quaternion.Euler(0, carYaw, 0));
                rear.localPosition = new Vector3(0, .1f, -1.4f + displaced); rear.localRotation = Quaternion.Euler(0, yaw, 0);
                front.localPosition = new Vector3(0, -.08f, .925f + displaced); front.localRotation = Quaternion.Euler(0, -yaw, 0);
                var p = resolveParts.Invoke(null, new[] { car });
                for (int i = 1; i <= 3; i++) Check(parts.GetMethod("AxleFor", All).Invoke(p, new object[] { i }) != null, "Authored native axle identity survives motion and track repositioning.");
            }
            var rail = Assembly.LoadFrom(Path.Combine(managed, "DV.RailTrack.dll")); var railType = rail.GetType("RailTrack", true);
            var track = Child(root.transform, "Straight endpoint fixture").gameObject.AddComponent(railType);
            var tracks = Array.CreateInstance(railType, 1); tracks.SetValue(track, 0);
            var pointSetType = railType.GetMethod("GetKinkedPointSet").ReturnType;
            var pointSet = Activator.CreateInstance(pointSetType); var pointType = pointSetType.GetField("points").FieldType.GetElementType();
            var ps = Array.CreateInstance(pointType, 17); var vd = pointType.GetField("position").FieldType;
            for (int i = 0; i < ps.Length; i++)
            {
                object p = Activator.CreateInstance(pointType);
                Set(p, "position", Activator.CreateInstance(vd, 0d, 0d, i * .5d)); Set(p, "forward", Vector3.forward); Set(p, "up", Vector3.up);
                Set(p, "index", i); Set(p, "span", i * .5d); Set(p, "spanToNextPoint", i == ps.Length - 1 ? 0d : .5d); ps.SetValue(p, i);
            }
            Set(pointSet, "points", ps); Set(pointSet, "span", 8d); Set(track, "pointSet", pointSet);
            var support = runtime.GetType("RearrangedS060.S060MeyerSupport", true); var pose = support.GetMethod("TrackPose", All);
            if (pose == null) throw new InvalidOperationException("Baseline lacks interpolated, endpoint-safe track placement.");
            foreach (float z in new[] { 0f, .137f, .672f, 1.9f, 4.249f, 7.91f })
            {
                var result = pose.Invoke(null, new object[] { new Vector3(0, 0, z), tracks });
                var traveller = result.GetType().GetField("Item2").GetValue(result);
                var actual = traveller.GetType().GetField("worldPosition").GetValue(traveller);
                double span = (double)traveller.GetType().GetProperty("Span").GetValue(traveller);
                Check(Math.Abs(span - z) < .00001, "Endpoint-safe native traveller retains exact requested span.");
            }
            bool rejected = false;
            try { pose.Invoke(null, new object[] { new Vector3(0, 0, -1.3282f), tracks }); } catch (TargetInvocationException e) { rejected = e.InnerException is InvalidOperationException; }
            Check(rejected, "A distant track endpoint is refused rather than displacing the bogie.");
            string hash; using (var sha = SHA256.Create()) using (var stream = File.OpenRead(dll)) hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            File.WriteAllText(Path.Combine(output, "switch-validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"runtimeSha256\":\"" + hash + "\",\"nativeAxlesAndTrackTraveller\":true,\"gameplayValidated\":false}");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "error.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); }
        finally { if (singleton != null) singleton.SetValue(null, previousRegistry); Object.DestroyImmediate(root); AppDomain.CurrentDomain.AssemblyResolve -= resolve; }
    }
}
#endif
