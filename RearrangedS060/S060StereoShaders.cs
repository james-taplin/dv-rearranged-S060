using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RearrangedS060
{
    // The Shay, Meyer and Dockside bundles were built with VR disabled, so the
    // Standard shader copy inside each has no single-pass stereo variants. In
    // VR those materials sample with the wrong eye transform and appear to swim
    // with head movement. The same Unity 2019.4.40f1 Standard, built once with
    // single-pass stereo enabled, ships in assets/Shared; the bundle materials
    // are pointed at it. Material properties, textures and keywords are kept.
    internal static class S060StereoShaders
    {
        internal const string Relative = "assets/Shared/s060_stereo_standard";
        private const string MaterialPath = "assets/s060stereo/s060 stereo standard.mat";
        private static AssetBundle bundle;
        private static Shader stereo; // static reference keeps it through UnloadUnusedAssets
        private static bool unavailable;
        private static readonly HashSet<GameObject> applied = new HashSet<GameObject>();

        internal static Shader Stereo
        {
            get
            {
                if (stereo != null || unavailable) return stereo;
                try
                {
                    if (bundle == null) bundle = AssetBundle.LoadFromFile(Path.Combine(Main.ModPath, "assets", "Shared", "s060_stereo_standard"));
                    var material = bundle != null ? bundle.LoadAsset<Material>(MaterialPath) : null;
                    stereo = material != null ? material.shader : null;
                    if (stereo == null || stereo.name != "Standard" || !stereo.isSupported)
                        throw new InvalidOperationException("The stereo Standard shader bundle is missing or invalid.");
                }
                catch (Exception ex)
                {
                    unavailable = true; stereo = null;
                    Main.Logger.LogException("S060 VR shader bundle unavailable; S060 bundle materials keep their original shader", ex);
                }
                return stereo;
            }
        }

        // Each asset root is scanned once per session (Preflight runs often).
        // Re-point every loaded material that uses the same bundle-local Standard
        // shader instance as the given asset roots. Idempotent and cheap once applied.
        internal static void Apply(params GameObject[] roots)
        {
            var shader = Stereo;
            if (shader == null) return;
            var sources = new HashSet<Shader>();
            foreach (var root in roots)
            {
                if (root == null || !applied.Add(root)) continue;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                        if (material != null && material.shader != null && material.shader != shader && material.shader.name == "Standard")
                            sources.Add(material.shader);
            }
            if (sources.Count == 0) return;
            int changed = 0;
            foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (material == null || material.shader == null || !sources.Contains(material.shader)) continue;
                Rebind(material, shader); changed++;
            }
            Main.Logger.Log("S060 VR shaders: " + changed + " bundle materials now use the single-pass stereo Standard shader.");
        }

        // Also used for run-time copies made from a bundle template.
        internal static void Rebind(Material material, Shader shader)
        {
            int queue = material.renderQueue;
            var keywords = material.shaderKeywords;
            material.shader = shader;
            material.shaderKeywords = keywords;
            material.renderQueue = queue;
        }
    }
}
