using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ShayBundleBuild
{
    [Serializable] public class Binding { public string path, role; public int index; public Vector3 restPosition; public Quaternion restRotation; }
    [Serializable] public class Motion
    {
        public Binding[] parts;
        public int ringTeeth,pinionTeeth,engineFitRevision;
        public float rollingRadius,engineBore,engineStroke,cylinderPitch,valveAxisX,truckShaftX,truckShaftY,memberLength,jointEndAllowance;
        public Vector3 rearEngineEnd,frontEngineEnd,rearTruckEnd,frontTruckEnd;
    }
    public static void RunBatch()
    {
        try
        {
            string output = Environment.GetEnvironmentVariable("S060_SHAY_BUNDLE_OUTPUT"); Directory.CreateDirectory(output);
            const string destination = "Assets/ShayIntegration";
            Directory.CreateDirectory(destination); AssetDatabase.Refresh();
            var source=Environment.GetEnvironmentVariable("S060_SHAY_SOURCE");
            if(string.IsNullOrEmpty(source))throw new InvalidOperationException("R3 authoring source is required.");
            var root=ShayR3ModelBuild.Create(source,out var motion);
            if(motion.parts.Length!=43)throw new InvalidOperationException("Exactly 43 original transmission bindings required.");
            File.WriteAllText(destination + "/motion.json", JsonUtility.ToJson(motion));
            foreach (var go in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
                if (go.GetComponents<MonoBehaviour>().Length != 0 || go.GetComponent<Collider>() != null || go.GetComponent<Rigidbody>() != null)
                    throw new InvalidOperationException("Asset-only module contains runtime/physics component: " + go.name);
            }
            // Native wheel aliases must remain empty until the real S060 is bound.
            var aliases = root.transform.Find("Trucks").GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh == null).ToArray();
            if (aliases.Length != 4) throw new InvalidOperationException("R2 needs one native pony wheelset placeholder per axle.");
            var prefabPath = destination + "/S060Shay.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            var audio = ShayExhaustBuild.Import(Environment.GetEnvironmentVariable("S060_SHAY_AUDIO_SOURCE"));
            var build = new AssetBundleBuild { assetBundleName = "shay_running_gear", assetNames = new[] { prefabPath, destination + "/motion.json" }.Concat(audio).ToArray() };
            if (BuildPipeline.BuildAssetBundles(output, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64) == null)
                throw new InvalidOperationException("Shay bundle build failed.");
            var bundle = AssetBundle.LoadFromFile(Path.Combine(output,"shay_running_gear"));
            var saved = bundle.LoadAsset<GameObject>(prefabPath);
            if (saved == null || saved.GetComponentsInChildren<MonoBehaviour>(true).Length != 0 ||
                saved.GetComponentsInChildren<Rigidbody>(true).Length != 0 || bundle.LoadAllAssets<TextAsset>().Length != 1 ||
                bundle.LoadAllAssets<AudioClip>().Length != 60)
                throw new InvalidOperationException("Reopened Shay bundle is incomplete.");
            string[] frames = saved.GetComponentsInChildren<Transform>(true).Where(t => t.name == "TRUCK_REAR" || t.name == "TRUCK_FRONT").Select(t=>t.name).ToArray();
            if (frames.Length != 2) throw new InvalidOperationException("Exactly two native truck attachment frames required.");
            var materials=saved.GetComponentsInChildren<MeshRenderer>(true).Select(r=>r.sharedMaterial).Where(m=>m!=null).ToArray();
            if(!materials.Any(m=>m.name=="S060_MACHINED")||materials.Any(m=>m.name.StartsWith("material_")))
                throw new InvalidOperationException("Reopened material identities must match the runtime finish slots.");
            File.WriteAllText(Path.Combine(output,"bundle-validation.json"), "{\"passed\":true,\"originalOnly\":true,\"reopenedBundle\":true,\"nativeWheelPlaceholders\":"+aliases.Length+",\"transmissionParts\":43,\"truckFrames\":2,\"runtimeComponents\":0}");
            bundle.Unload(true); UnityEngine.Object.DestroyImmediate(root); EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
