using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DV.ModularAudioCar;
using HarmonyLib;
using UnityEngine;

namespace RearrangedS060
{
    // Native TrainAudio is pooled beneath the detached interior. Do not search
    // the world or move its controller: only move the cylinder emitter holders,
    // and restore them before native audio returns to its pool.
    internal static class S060NativeAudio
    {
        private static readonly ConditionalWeakTable<TrainCar, Placement> Placements = new ConditionalWeakTable<TrainCar, Placement>();
        internal static IEnumerable<T> Components<T>(TrainCar car) where T : Component
        {
            var body = car.GetComponentsInChildren<T>(true);
            return car.interior == null ? body : body.Concat(car.interior.GetComponentsInChildren<T>(true)).Distinct();
        }
        internal static void Register(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(TrainAudio), nameof(TrainAudio.SetupForCar)),
                postfix: new HarmonyMethod(typeof(S060NativeAudio), nameof(AudioReady)));
            harmony.Patch(AccessTools.Method(typeof(TrainAudio), "ReturnToPool"),
                prefix: new HarmonyMethod(typeof(S060NativeAudio), nameof(AudioReturning)));
        }
        private static void AudioReady(TrainAudio __instance, TrainCar trainCar)
        { if (trainCar != null && Placements.TryGetValue(trainCar, out var owner)) owner.Add(__instance.transform); }
        private static void AudioReturning(TrainAudio __instance, TrainCar ___car)
        { if (___car != null && Placements.TryGetValue(___car, out var owner)) owner.Restore(__instance.transform); }

        internal static Placement Translate(TrainCar car, Vector3 offset) => new Placement(car, offset, null, true);
        internal static Placement Follow(TrainCar car, Vector3 offset, Transform frame) => new Placement(car, offset, frame, false);
        internal sealed class Placement : IDisposable
        {
            private readonly TrainCar car;
            private readonly Vector3 offset;
            private readonly Transform frame;
            private readonly bool cocks;
            private readonly List<Home> homes = new List<Home>();
            internal Placement(TrainCar car, Vector3 offset, Transform frame, bool cocks)
            {
                this.car = car; this.offset = offset; this.frame = frame; this.cocks = cocks;
                Placements.Add(car, this);
                try { Add(car.transform); if (car.interior != null) Add(car.interior); }
                catch { Dispose(); throw; }
            }
            internal void Add(Transform root)
            {
                var sources = root.GetComponentsInChildren<Transform>(true).Where(t =>
                    t.name == "SteamRelease_Layered" && t.parent != null && t.parent.name == "CylinderCrack").ToList();
                if (cocks) sources.AddRange(root.GetComponentsInChildren<CylinderCockLayeredPortReader>(true)
                    .Where(r => r.crankRotationPortId == "steamEngine.CRANK_ROTATION")
                    .SelectMany(r => r.cylCockAudio).Where(a => a != null).Select(a => a.transform));
                var unique = sources.Where(t => t != null).Distinct().ToArray();
                foreach (var part in unique.Where(t => !unique.Any(p => p != t && t.IsChildOf(p))))
                {
                    if (homes.Any(h => h.Part == part)) continue;
                    homes.Add(new Home(part));
                    if (frame == null) part.position += car.transform.TransformVector(offset);
                    else
                    {
                        var position = car.transform.InverseTransformPoint(part.position);
                        var rotation = Quaternion.Inverse(car.transform.rotation) * part.rotation;
                        var scale = part.lossyScale; var carScale = car.transform.lossyScale;
                        part.SetParent(frame, false); part.localPosition = position + offset; part.localRotation = rotation;
                        part.localScale = new Vector3(scale.x / carScale.x, scale.y / carScale.y, scale.z / carScale.z);
                    }
                }
            }
            internal void Restore(Transform root)
            {
                foreach (var home in homes.Where(h => root == null || h.Parent == root ||
                    (h.Parent != null && h.Parent.IsChildOf(root))).ToArray())
                { home.Restore(); homes.Remove(home); }
            }
            public void Dispose() { Placements.Remove(car); Restore(null); }
            private sealed class Home
            {
                internal readonly Transform Part, Parent;
                private readonly Vector3 position, scale;
                private readonly Quaternion rotation;
                internal Home(Transform part)
                { Part = part; Parent = part.parent; position = part.localPosition; rotation = part.localRotation; scale = part.localScale; }
                internal void Restore()
                {
                    if (Part == null) return;
                    Part.SetParent(Parent, false); Part.localPosition = position; Part.localRotation = rotation; Part.localScale = scale;
                }
            }
        }
    }
}
