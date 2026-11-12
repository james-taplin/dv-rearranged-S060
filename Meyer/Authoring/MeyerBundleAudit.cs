using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CCL.Types;
using CCL.Types.Components;
using CCL.Types.Proxies.Audio;
using CCL.Types.Proxies.Ports;
using CCL.Types.Proxies.VFX;
using CCL.Types.Proxies.Wheels;
using UnityEngine;

public static class MeyerBundleAudit
{
    public static void Validate(string path, string report)
    {
        string bundleHash = Hash(path);
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null) throw new InvalidOperationException("Cannot reopen Meyer bundle.");
        try
        {
            var packs = bundle.LoadAllAssets<CustomCarPack>();
            Require(packs.Length == 1 && packs[0].Cars.Length == 1, "One CCL pack and canonical car required.");
            var type = packs[0].Cars[0];
            Require(type.IsSteamLocomotive && !type.useDefaultWheelRotation, "Steam type and powered rotation required.");
            Require(type.liveries.Count == 1 && type.liveries[0].id == MeyerPlan.CarId, "One canonical Meyer livery required.");
            var root = type.liveries[0].prefab;
            SteamPipeAuthoring.Validate(root);
            SteamNativePinAudit.Validate(root, Path.GetDirectoryName(report), false);
            var front = root.transform.Find("BogieF/bogie_car"); var rear = root.transform.Find("BogieR/bogie_car");
            Require(Mathf.Abs(root.transform.InverseTransformPoint(front.position).z - MeyerPlan.FrontCentre) < .0001f && Mathf.Abs(root.transform.InverseTransformPoint(rear.position).z - MeyerPlan.RearCentre) < .0001f, "Meyer needs two axles below the S060 and two in its attached rear section.");
            Require(front.Cast<Transform>().Count(t => t.name.StartsWith("[axle]")) == 2 && rear.Cast<Transform>().Count(t => t.name.StartsWith("[axle]")) == 2, "Two physical axles per powered unit required.");
            Require(front.GetComponent<AnimatorPortReaderProxy>().portId == MeyerSteamAuthoring.Hp + ".CRANK_ROTATION" && rear.GetComponent<AnimatorPortReaderProxy>().portId == MeyerSteamAuthoring.Lp + ".CRANK_ROTATION", "HP belongs under the loco; LP belongs on the attached rear unit.");
            Require(rear.Find("[Attached rear platform]") != null && rear.Find("[Meyer rear pannier L]") != null && rear.Find("[Meyer rear pannier R]") != null, "Complete attached rear platform/panniers required.");
            Require(rear.Find("[Period rear chassis]")?.GetComponent<MeshFilter>()?.sharedMesh != null, "Period rear frame details are missing.");
            Require(new[] { front, rear }.SelectMany(t => t.GetComponentsInChildren<MeshRenderer>(true)).All(r => r.GetComponent<MaterialGrabberRenderer>() != null), "Every Meyer power-unit renderer needs the live S060 material alias.");
            Require(Mathf.Abs(root.transform.Find("[coupler_rig_rear]").localPosition.z - MeyerPlan.RearCouplerZ) < .0001f, "Rear coupling anchor must be behind the rear steam bogie.");
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true).OfType<ICustomSerialized>()) component.AfterImport();
            MeyerSteamAuthoring.Validate(root);
            var graph = root.GetComponentInChildren<SimConnectionsDefinitionProxy>(true);
            Require(graph.executionOrder.Select(c => c.ID).Distinct().Count() == graph.executionOrder.Count,
                "Execution IDs must be unique: " + string.Join(",", graph.executionOrder.GroupBy(c => c.ID).Where(g => g.Count() > 1).Select(g => g.Key + " x" + g.Count())));
            var manager = root.GetComponent<PoweredWheelsManagerProxy>();
            Require(manager != null && !manager.GetWheelsFromDefaultBogies && manager.poweredWheels.Length == 4 && manager.poweredWheels.All(w => w != null && w.wheelTransform != null), "Four serialized powered axles required.");
            Require(root.GetComponent<PoweredWheelRotationViaCodeProxy>() == null && root.GetComponentsInChildren<AnimatorPortReaderProxy>(true).Length == 2, "Two native crank animators required without competing code rotation.");
            var controllers = root.GetComponentsInChildren<ParticlesPortReadersControllerProxy>(true);
            Require(controllers.Length == 2 && controllers.All(c => !c.selfInitialization && c.particlePortReaders.Count == 2), "Two serialized drip/controller bindings required.");
            foreach (var controller in controllers)
            {
                Require(controller.transform.parent.name == "bogie_car", "Effects must follow the actual bogie frame.");
                Require(controller.particlePortReaders.All(p => p.particleUpdaters.Count == 2 && p.particleUpdaters.Any(u => u.portId == "cylinderCock.EXT_IN")), "Each drip needs water and cab-control gating.");
            }
            Require(root.GetComponentsInChildren<CylinderCockLayeredPortReaderProxy>(true).Length == 2, "Two sound readers required.");
            Require(root.GetComponentsInChildren<CopyVanillaAudioSystem>(true).Count(a => a.AudioSystem == VanillaAudioSystem.SteamerCylinderCock) == 4, "Four native cylinder sound aliases required.");
            Require(bundle.LoadAllAssets<AudioClip>().Length == 0, "Game audio must remain native aliases.");
            Require(!bundle.LoadAllAssets<Mesh>().Any(m => m.name.StartsWith("s060", StringComparison.OrdinalIgnoreCase)), "Native S060 meshes must not be bundled.");
            MeyerMotionAudit.Validate(root, Path.GetDirectoryName(report));
            SteamPipeAudit.Validate(root, Path.GetDirectoryName(report));
            SteamEffectsAudit.Validate(root, Path.GetDirectoryName(report), true);
            Require(Hash(path) == bundleHash, "Bundle changed while its reopened content was being validated.");
            File.WriteAllText(report, "{\"passed\":true,\"reopenedBundle\":true,\"bundleSha256\":\"" + bundleHash + "\",\"canonicalCars\":1,\"canonicalLiveries\":1,\"poweredAxles\":4,\"cockReaders\":2,\"waterDrips\":4,\"cockSoundAliases\":4,\"bundledGameMeshes\":0,\"bundledAudioClips\":0,\"runtimeValidated\":false}\n");
        }
        finally { bundle.Unload(true); }
    }
    private static string Hash(string path)
    { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
