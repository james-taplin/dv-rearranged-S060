using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CCL.Types;
using UnityEditor;
using UnityEngine;

// Reopen the actual UMM-extracted nested bundles. This tests packaging and
// serialized donor availability, not CCL's world processing or a driving scene.
public static class CombinedPackageAudit
{
    private static int checks;
    public static void RunBatch()
    {
        string root = Environment.GetEnvironmentVariable("S060_COMBINED_PACKAGE");
        string output = Environment.GetEnvironmentVariable("S060_COMBINED_REPORT");
        try
        {
            Require(Directory.Exists(root), "UMM-extracted combined mod folder required.");
            Require(Directory.GetFiles(root, "Info.json", SearchOption.AllDirectories).Length == 1, "One UMM identity required.");
            var paths = Directory.GetFiles(root, "ccl_bundle", SearchOption.AllDirectories).OrderBy(p => p).ToArray();
            Require(paths.Length == 2, "CCL recursive discovery must find exactly two bundles.");
            var ids = new List<string>(); var hashes = new List<string>();
            foreach (string path in paths)
            {
                string expected = Path.GetFileName(Path.GetDirectoryName(path)) == "Meyer" ? "RS060_Meyer_0440" : "RS060_Franklin_Dockside_0440";
                string hash = Hash(path); var bundle = AssetBundle.LoadFromFile(path);
                Require(bundle != null, "Native Unity bundle reopen failed: " + path);
                try
                {
                    var packs = bundle.LoadAllAssets<CustomCarPack>();
                    Require(packs.Length == 1 && packs[0].Cars.Length == 1, "One canonical donor type per bundle required.");
                    var car = packs[0].Cars[0];
                    Require(car.liveries.Count == 1 && car.liveries[0].id == expected, "Existing saved/donor livery ID must be retained.");
                    Require(car.liveries[0].prefab != null, "Serialized donor prefab missing.");
                    var frames = car.liveries[0].prefab.GetComponentsInChildren<Transform>(true).Where(t => t.name == "bogie_car").ToArray();
                    Require(frames.Length == 2, "Each radio arrangement requires two existing powered frames.");
                    foreach (var frame in frames)
                    {
                        Require(frame.GetComponent<Animator>() != null, "Powered frame animator missing.");
                        Require(frame.Find("[S060 fitted effects 0.7]") != null && frame.Find("[S060 fitted gear 0.6]") != null, "Runtime preflight markers missing.");
                        Require(frame.Cast<Transform>().Count(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)) == 2, "Two driven axles per frame required.");
                    }
                    Require(bundle.LoadAllAssets<AudioClip>().Length == 0, "No native or Verde audio may be distributed.");
                    Require(!bundle.LoadAllAssets<Mesh>().Any(m => m.name.StartsWith("s060", StringComparison.OrdinalIgnoreCase)), "No native S060 meshes may be distributed.");
                    Require(Hash(path) == hash, "Bundle changed while reopening.");
                    ids.Add(car.liveries[0].id); hashes.Add(hash);
                }
                finally { bundle.Unload(true); }
            }
            Require(ids.Distinct().Count() == 2, "Donor IDs must remain distinct.");
            string shayPath=Path.Combine(root,"assets","Shay","shay_running_gear");
            var shay=AssetBundle.LoadFromFile(shayPath);
            Require(shay!=null,"The extracted original Shay model bundle must reopen.");
            try
            {
                var prefab=shay.LoadAsset<GameObject>("assets/shayintegration/s060shay.prefab");
                Require(prefab!=null,"Shay prefab must be addressable by the runtime asset path.");
                Require(prefab.GetComponentsInChildren<MeshFilter>(true).Count(f=>f.sharedMesh==null&&f.name.EndsWith("_NATIVE_SET"))==4,"R2: exactly four pony-wheelset placeholders remain unbundled.");
                Require(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length==0 && prefab.GetComponentsInChildren<Collider>(true).Length==0,"Original Shay bundle has no runtime scripts or physics.");
                Require(shay.LoadAllAssets<TextAsset>().Length==1,"Transmission binding table must be present.");
                Require(shay.LoadAllAssets<CustomCarPack>().Length==0 && shay.LoadAllAssets<Texture>().Length==0 && shay.LoadAllAssets<AudioClip>().Length==0,"Shay adds no donor car, native textures or audio.");
            }
            finally {shay.Unload(true);}
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(new Report { passed = true, checks = checks, reopenedNestedBundles = true,
                existingRadioDonorsAvailable = true, shayModelsAvailable=true, shayBundleSha256=Hash(shayPath), liveryIds = ids.ToArray(), bundleHashes = hashes.ToArray() }, true));
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
    private static string Hash(string path)
    { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
    [Serializable] private sealed class Report
    {
        public bool passed, reopenedNestedBundles, existingRadioDonorsAvailable, shayModelsAvailable;
        public bool gameRuntimeValidated = false, nativeWorldImportValidated = false;
        public int checks; public string[] liveryIds, bundleHashes;
        public string shayBundleSha256;
    }
}
