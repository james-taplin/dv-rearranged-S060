using System;
using System.IO;
using UnityEngine;

namespace RearrangedS060
{
    // The original bundle is retained for the lifetime of its immutable assets.
    // Each locomotive instantiates its own geometry; materials/controllers here
    // are never edited or instantiated as scene roots.
    internal static class AssetBundleLoader
    {
        private static AssetBundle drivetrain;
        private static T Load<T>(string name) where T : UnityEngine.Object
        {
            if (drivetrain == null)
            {
                string path = Path.Combine(Main.ModPath, "assets", "s282_drivetrain");
                if (!File.Exists(path)) throw new InvalidOperationException("Articulated drivetrain bundle is missing: assets/s282_drivetrain");
                drivetrain = AssetBundle.LoadFromFile(path);
                if (drivetrain == null) throw new InvalidOperationException("The articulated drivetrain bundle could not be loaded.");
            }
            var asset = drivetrain.LoadAsset<T>(name);
            if (asset == null) throw new InvalidOperationException("Articulated drivetrain asset is missing: " + name);
            return asset;
        }
        internal static GameObject BranchPipe => Load<GameObject>("branch_pipe");
        internal static GameObject BranchPipe4444 => Load<GameObject>("branch_pipe_4444");
        internal static GameObject FranklinBValveGear => Load<GameObject>("franklin_type_b");
        internal static GameObject FranklinBValveGear4444 => Load<GameObject>("franklin_type_b_4444");
        internal static GameObject FranklinBReverseGear => Load<GameObject>("franklin_type_b_reverse");
        internal static GameObject FranklinBReverseGear4444 => Load<GameObject>("franklin_type_b_reverse_4444");
        internal static GameObject FranklinBReverseGearRear => Load<GameObject>("franklin_type_b_reverse_rear");
        internal static RuntimeAnimatorController FranklinBAnimCtrl => Load<RuntimeAnimatorController>("franklin_b_anim_ctrl");
        internal static RuntimeAnimatorController FranklinBAnimCtrl4444 => Load<RuntimeAnimatorController>("franklin_b_anim_ctrl_4444");
        internal static void ValidateArticulatedAssets()
        {
            _ = BranchPipe; _ = BranchPipe4444; _ = FranklinBValveGear; _ = FranklinBValveGear4444;
            _ = FranklinBReverseGear; _ = FranklinBReverseGear4444; _ = FranklinBReverseGearRear;
            _ = FranklinBAnimCtrl; _ = FranklinBAnimCtrl4444;
        }
    }
}
