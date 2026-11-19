using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Builds assets/Shared/s060_stereo_standard: one material carrying Unity's
// built-in Standard shader compiled with single-pass stereo enabled, the VR
// mode Derail Valley ships. The Shay/Meyer/Dockside bundles were built with VR
// disabled, which strips those variants; the runtime rebinds their materials.
public static class StereoShaderBuild
{
    public static void RunBatch()
    {
        try
        {
            string output = Environment.GetEnvironmentVariable("S060_STEREO_OUTPUT");
            Directory.CreateDirectory(output);
            var group = BuildTargetGroup.Standalone;
#pragma warning disable 618
            PlayerSettings.SetVirtualRealitySupported(group, true);
            UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(group, new[] { "None" });
#pragma warning restore 618
            PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;
#pragma warning disable 618
            if (!PlayerSettings.GetVirtualRealitySupported(group) || PlayerSettings.stereoRenderingPath != StereoRenderingPath.SinglePass)
#pragma warning restore 618
                throw new InvalidOperationException("Single-pass VR could not be enabled for the shader build.");
            const string folder = "Assets/S060Stereo";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            const string path = folder + "/S060 Stereo Standard.mat";
            var material = new Material(Shader.Find("Standard")) { name = "S060 Stereo Standard" };
            AssetDatabase.CreateAsset(material, path); AssetDatabase.SaveAssets();
            var build = new AssetBundleBuild { assetBundleName = "s060_stereo_standard", assetNames = new[] { path } };
            if (BuildPipeline.BuildAssetBundles(output, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64) == null)
                throw new InvalidOperationException("Stereo shader bundle build failed.");
            var bundle = AssetBundle.LoadFromFile(Path.Combine(output, "s060_stereo_standard"));
            var saved = bundle.LoadAsset<Material>("assets/s060stereo/s060 stereo standard.mat");
            if (saved == null || saved.shader == null || saved.shader.name != "Standard") throw new InvalidOperationException("Reopened stereo material is invalid.");
            bundle.Unload(true);
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
