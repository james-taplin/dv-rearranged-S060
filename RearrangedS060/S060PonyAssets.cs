using System;
using System.Linq;
using DV;
using DV.Wheels;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using LocoSim.Implementations.Wheels;
using UnityEngine;

namespace RearrangedS060
{
    // Read the registered vanilla prefab. Never instantiate the locomotive,
    // edit its definition or export/copy game meshes into the public package.
    internal sealed class S060PonyAssets
    {
        internal Transform Axle, Root, Support;
        internal float Radius;
        internal BezierCurve.Axis RotationAxis;
        internal bool AffectedBySlide;

        internal static S060PonyAssets Resolve()
        {
            var types = Globals.G?.Types;
            if (types == null) throw new InvalidOperationException("Native vehicle registry is not ready.");
            // Use DV's stock enum-to-livery mapping, not guessed livery IDs.
            foreach (var livery in new[] { TrainCarType.LocoSteamHeavy.ToV2() })
            {
                if (livery == null) continue;
                var prefab = livery.prefab;
                if (prefab == null || prefab.transform.Find("LocoS282A_Body") == null) continue;
                foreach (var axle in prefab.GetComponentsInChildren<Transform>(true))
                {
                    if (!axle.name.StartsWith("[axle]", StringComparison.Ordinal) || axle.GetComponent<PoweredWheel>() != null) continue;
                    if (!axle.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh != null &&
                        f.sharedMesh.name.StartsWith("s282_wheels_front", StringComparison.OrdinalIgnoreCase))) continue;
                    var rotator = prefab.GetComponentsInChildren<WheelRotationViaCode>(true).FirstOrDefault(r =>
                        r.transformsToRotate != null && r.transformsToRotate.Contains(axle));
                    if (rotator == null || !(rotator.wheelRadius > 0f)) continue;
                    // Guard against cloning a native behaviour with references
                    // to its donor car. Plain axle presentation is all we need.
                    if (axle.GetComponentsInChildren<Component>(true).Any(c => c != null &&
                        !(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer) && !(c is LODGroup)))
                        throw new InvalidOperationException("S282 pony axle has unexpected native behaviours; conversion refused.");
                    return new S060PonyAssets
                    {
                        Axle = axle, Root = prefab.transform, Radius = rotator.wheelRadius,
                        RotationAxis = rotator.rotationAxis, AffectedBySlide = rotator.affectedByWheelSlide,
                        Support = prefab.transform.Find("LocoS282A_Body/Static_LOD0/s282_wheels_front_support")
                    };
                }
            }
            throw new InvalidOperationException("The registered stock S282 pony axle is unavailable; no changes were made to the S060.");
        }
    }
}
