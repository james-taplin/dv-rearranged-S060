using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ShayBundleBuild
{
    [Serializable] public class Binding { public string path, role; public int index; public Vector3 restPosition; public Quaternion restRotation; }
    [Serializable] public class Motion { public Binding[] parts; }
    public static void RunBatch()
    {
        try
        {
            string output = Environment.GetEnvironmentVariable("S060_SHAY_BUNDLE_OUTPUT"); Directory.CreateDirectory(output);
            const string destination = "Assets/ShayIntegration";
            Directory.CreateDirectory(destination); AssetDatabase.Refresh();
            var root = new GameObject("S060 Shay R1");
            GameObject Load(string path, string name)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new InvalidOperationException("Missing original module: " + path);
                var item = UnityEngine.Object.Instantiate(prefab, root.transform); item.name = name; return item;
            }
            var engine = Load("Assets/ShaySources/Engine/S060_SHAY_ENGINE_R1.prefab", "Engine");
            engine.transform.localPosition = new Vector3(1.10f,.52f,.11f); engine.transform.localRotation = Quaternion.identity; // R2 engine origin
            var trucks = Load("Assets/ShaySources/Trucks/Prefabs/S060-Shay-Trucks.prefab", "Trucks");
            var transmission = Load("Assets/ShaySources/Transmission/S060-Shay-Transmission-STUDY.prefab", "Transmission");
            var preview = transmission.GetComponent<ShayTransmissionPreview>();
            if (preview == null || preview.parts.Length != 43) throw new InvalidOperationException("Verified transmission rig required.");
            var motion = new Motion { parts = preview.parts.Select(p => new Binding {
                path = AnimationUtility.CalculateTransformPath(p.transform, transmission.transform), role = p.role, index = p.index,
                restPosition = p.restPosition, restRotation = p.restRotation }).ToArray() };
            File.WriteAllText(destination + "/motion.json", JsonUtility.ToJson(motion));
            UnityEngine.Object.DestroyImmediate(preview);
            foreach (var go in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
                if (go.GetComponents<MonoBehaviour>().Length != 0 || go.GetComponent<Collider>() != null || go.GetComponent<Rigidbody>() != null)
                    throw new InvalidOperationException("Asset-only module contains runtime/physics component: " + go.name);
            }
            // Native wheel aliases must remain empty until the real S060 is bound.
            var aliases = trucks.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh == null).ToArray();
            if (aliases.Length != 4) throw new InvalidOperationException("R2 needs one native pony wheelset placeholder per axle.");
            var prefabPath = destination + "/S060Shay.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            var build = new AssetBundleBuild { assetBundleName = "shay_running_gear", assetNames = new[] { prefabPath, destination + "/motion.json" } };
            if (BuildPipeline.BuildAssetBundles(output, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64) == null)
                throw new InvalidOperationException("Shay bundle build failed.");
            var bundle = AssetBundle.LoadFromFile(Path.Combine(output,"shay_running_gear"));
            var saved = bundle.LoadAsset<GameObject>(prefabPath);
            if (saved == null || saved.GetComponentsInChildren<MonoBehaviour>(true).Length != 0 ||
                saved.GetComponentsInChildren<Rigidbody>(true).Length != 0 || bundle.LoadAllAssets<TextAsset>().Length != 1)
                throw new InvalidOperationException("Reopened Shay bundle is incomplete.");
            string[] frames = saved.GetComponentsInChildren<Transform>(true).Where(t => t.name == "TRUCK_REAR" || t.name == "TRUCK_FRONT").Select(t=>t.name).ToArray();
            if (frames.Length != 2) throw new InvalidOperationException("Exactly two native truck attachment frames required.");
            File.WriteAllText(Path.Combine(output,"bundle-validation.json"), "{\"passed\":true,\"originalOnly\":true,\"reopenedBundle\":true,\"nativeWheelPlaceholders\":"+aliases.Length+",\"transmissionParts\":43,\"truckFrames\":2,\"runtimeComponents\":0}");
            bundle.Unload(true); UnityEngine.Object.DestroyImmediate(root); EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
