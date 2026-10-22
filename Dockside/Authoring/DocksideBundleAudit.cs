using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CCL.Types;
using CCL.Types.Components;
using CCL.Types.Proxies.Audio;
using CCL.Types.Proxies.Controllers;
using CCL.Types.Proxies.Ports;
using CCL.Types.Proxies.VFX;
using CCL.Types.Proxies.Wheels;
using UnityEngine;

public static class DocksideBundleAudit
{
    public static void Validate(string path, string report)
    {
        string bundleHash = Hash(path);
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null) throw new InvalidOperationException("Cannot reopen dockside CCL bundle.");
        try
        {
            var pack = bundle.LoadAllAssets<CustomCarPack>().Single();
            Require(pack.Cars.Length == 1 && pack.Cars[0].liveries.Count == 1, "One canonical car/livery required.");
            var type = pack.Cars[0]; var variant = type.liveries[0]; var root = variant.prefab;
            SteamPipeAuthoring.Validate(root);
            SteamNativePinAudit.Validate(root, Path.GetDirectoryName(report), true);
            Require(variant.id == DocksidePlan.CarId && type.IsSteamLocomotive && !type.useDefaultWheelRotation && type.wheelRadius == DocksidePlan.DriverRadius, "Dockside type/rotation configuration changed.");
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true).OfType<ICustomSerialized>()) component.AfterImport();
            DocksideSteamAuthoring.Validate(root);
            var graph = root.GetComponentInChildren<SimConnectionsDefinitionProxy>(true);
            var wheels = root.GetComponent<PoweredWheelsManagerProxy>();
            Require(wheels != null && !wheels.GetWheelsFromDefaultBogies && wheels.poweredWheels.Length == 4, "Four explicitly powered axles required.");
            Require(root.GetComponent<PoweredWheelRotationViaCodeProxy>() == null, "Crank animation must not compete with code rotation.");
            foreach (var wheel in wheels.poweredWheels)
            {
                Require(wheel != null && wheel.wheelTransform != null && wheel.wheelTransform.name == "Small driver R (native alias)" && Mathf.Abs(wheel.transform.localPosition.y - DocksidePlan.DriverRadius) < .0001f, "Powered wheel must reference its animated visual wheel at the physical axle centre.");
                foreach (Transform mesh in wheel.transform)
                    Require(Mathf.Abs(mesh.localScale.x) == 1 && Mathf.Abs(mesh.localScale.y - DocksidePlan.DriverRadius / DocksidePlan.NativeDriverRadius) < .0001f && mesh.localScale.y == mesh.localScale.z, "Driver diameter must not scale gauge or width.");
            }
            var gear = root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Franklin fit ", StringComparison.Ordinal)).ToArray();
            Require(gear.Length == 4 && gear.All(t => t.parent.name == "bogie_car"), "Both Franklin pairs must follow their actual engine units.");
            Require(gear.All(t => Mathf.Abs(Mathf.Abs(t.localScale.x) - t.localScale.y) < .0001f), "Franklin shaft cross-sections must not be stretched across the gauge.");
            Require(root.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("[Franklin fitted supports ", StringComparison.Ordinal)) == 4, "Every reused gear assembly requires its fitted axle and valve supports.");
            Require(root.GetComponentsInChildren<MeshFilter>(true).Count(f => f.name == "Main rod" && f.sharedMesh != null && f.sharedMesh.name.StartsWith("DocksideRod", StringComparison.Ordinal)) == 4, "Four shaped main rods required.");
            Require(gear.All(t => !t.Find("cylinder").GetComponentsInChildren<Renderer>(true).Any(r => SteamBogieVisuals.IsCylinderHousing(r, t.Find("cylinder")))), "Old cuboid cylinder housing geometry must be absent.");
            Require(root.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("[Round cylinders ", StringComparison.Ordinal)) == 4, "Four round cylinder assemblies required.");
            Require(!root.GetComponentsInChildren<Transform>(true).Any(t => t.name.IndexOf("pannier", StringComparison.OrdinalIgnoreCase) >= 0), "No rear pannier/extension requested for the dockside.");
            var controllers = root.GetComponentsInChildren<ParticlesPortReadersControllerProxy>(true);
            Require(controllers.Length == 2 && controllers.All(c => !c.selfInitialization && c.particlePortReaders.Count == 2 && c.transform.parent.name == "bogie_car"), "Both native cock/drip systems must follow the units.");
            Require(root.GetComponentsInChildren<CylinderCockParticlePortReaderProxy>(true).Length == 2 && root.GetComponentsInChildren<CylinderCockLayeredPortReaderProxy>(true).Length == 2, "Two native particle/audio readers required.");
            Require(root.GetComponentsInChildren<CopyVanillaAudioSystem>(true).Length == 4, "Four cylinder-cock sound aliases required.");
            Require(bundle.LoadAllAssets<AudioClip>().Length == 0 && bundle.LoadAllAssets<Texture2D>().Length == 0, "No game audio/textures may be bundled.");
            // CCL exports the pack as the root asset; dependency meshes need
            // to be followed through the loaded prefab, not LoadAllAssets alone.
            var meshes = root.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).Where(m => m != null).Concat(bundle.LoadAllAssets<Mesh>()).Distinct().ToArray();
            var franklinMeshes = meshes.Where(m => m.name.StartsWith("FranklinCustomMesh", StringComparison.Ordinal)).ToArray();
            // Cube/Cylinder are built-in Unity shapes used by SDK couplers and
            // dummy plates; the other four are supplied by the existing SDK.
            var sdkMeshes = new[] { "Buffer07", "HookPlate", "CableBase_01", "HeadEndPowerConnectorBase", "Cube", "Cylinder" };
            Require(franklinMeshes.Length > 0 && meshes.All(m => franklinMeshes.Contains(m) || sdkMeshes.Contains(m.name) || m.name.StartsWith("DocksideRod") || m.name.StartsWith("SteamManifold") || m.name.StartsWith("SteamTransfer")), "Unexpected mesh outside custom Franklin geometry and standard SDK coupler models: " + string.Join(",", meshes.Select(m => m.name)));
            bool first = true; Bounds bounds = default(Bounds);
            foreach (var filter in gear.SelectMany(t => t.GetComponentsInChildren<MeshFilter>(true)).Where(f => f.GetComponent<Renderer>()?.enabled == true))
            {
                var b = filter.sharedMesh.bounds;
                foreach (float x in new[] { b.min.x, b.max.x }) foreach (float y in new[] { b.min.y, b.max.y }) foreach (float z in new[] { b.min.z, b.max.z })
                {
                    var p = root.transform.InverseTransformPoint(filter.transform.TransformPoint(new Vector3(x, y, z)));
                    if (first) { bounds = new Bounds(p, Vector3.zero); first = false; } else bounds.Encapsulate(p);
                }
            }
            Require(!first && bounds.min.z >= -4.295f && bounds.max.z <= 4.004f && bounds.min.x >= -1.672f && bounds.max.x <= 1.672f, "Franklin fit extends outside the S060 body envelope: " + bounds);
            DocksideMotionAudit.Validate(root, Path.GetDirectoryName(report));
            SteamPipeAudit.Validate(root, Path.GetDirectoryName(report));
            SteamEffectsAudit.Validate(root, Path.GetDirectoryName(report), false);
            Require(Hash(path) == bundleHash, "Bundle changed while its reopened content was being validated.");
            File.WriteAllText(report, "{\"passed\":true,\"reopenedBundle\":true,\"bundleSha256\":\"" + bundleHash + "\",\"nativeSimpleEngines\":2,\"poweredAxles\":4,\"driverDiameter\":1.0,\"wheelWidthScaled\":false,\"franklinAssemblies\":4,\"allGearUnderLoco\":true,\"bothBoilerIntakesSummed\":true,\"bothExhaustsSummed\":true,\"cockReaders\":2,\"cockSoundAliases\":4,\"customFranklinMeshes\":" + franklinMeshes.Length + ",\"sdkCouplerMeshes\":" + (meshes.Length - franklinMeshes.Length) + ",\"nativeBodyAndWheelMeshesBundled\":0,\"runtimeValidated\":false,\"franklinBoundsMin\":[" + Vec(bounds.min) + "],\"franklinBoundsMax\":[" + Vec(bounds.max) + "]}\n");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(report), "native-wiring.json"),
                "{\"references\":[" + string.Join(",", graph.portReferenceConnections.Select(c => "{\"reference\":\"" + c.portReferenceId + "\",\"source\":\"" + c.portId + "\"}")) +
                "],\"execution\":[" + string.Join(",", graph.executionOrder.Select(c => "\"" + c.ID + "\"")) + "]}\n");
        }
        finally { bundle.Unload(true); }
    }
    private static string Hash(string path)
    { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
    private static string Vec(Vector3 v) => string.Join(",", new[] { v.x, v.y, v.z }.Select(x => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
