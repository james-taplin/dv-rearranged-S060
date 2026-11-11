#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // Keep the original renderers in their native paint/LOD lists. Move the
    // complete stock castings, never scale or rebake their shared geometry.
    internal sealed class S060NativeCylinders : IDisposable
    {
        private readonly List<(Transform part, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)> homes =
            new List<(Transform, Transform, Vector3, Quaternion, Vector3)>();
        private readonly List<(Renderer renderer, bool enabled)> fallback = new List<(Renderer, bool)>();
        private S060NativeAudio.Placement audio;
        internal static S060NativeCylinders Bind(S060Parts parts, Transform front)
        {
            var owner = new S060NativeCylinders();
            try
            {
                var source = parts.Body.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == "s060_cylinder_left" || t.name == "s060_cylinder_right").ToArray();
                if (source.Length < 2) throw new InvalidOperationException("Full native S060 cylinder castings are unavailable.");
                var move = source.ToList();
                var crack = parts.Car.transform.Find("LocoS060_Particles/CylinderCrack"); if (crack != null) move.Add(crack);
                foreach (var part in move.Distinct())
                {
                    owner.homes.Add((part, part.parent, part.localPosition, part.localRotation, part.localScale));
                    var position = parts.Car.transform.InverseTransformPoint(part.position);
                    var rotation = Quaternion.Inverse(parts.Car.transform.rotation) * part.rotation;
                    var scale = part.lossyScale; var carScale = parts.Car.transform.lossyScale;
                    part.SetParent(front, false);
                    part.localPosition = position + new Vector3(0, 0, S060MeyerLayout.FrontCylinderZ - S060PipeGeometry.NativeCylinderCentreZ);
                    part.localRotation = rotation;
                    part.localScale = new Vector3(scale.x / carScale.x, scale.y / carScale.y, scale.z / carScale.z);
                    if (source.Contains(part)) foreach (var renderer in part.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
                }
                foreach (string side in new[] { "L", "R" })
                {
                    var old = front.Find("[Round cylinders " + side + "]");
                    if (old == null) continue;
                    foreach (var renderer in old.GetComponentsInChildren<Renderer>(true))
                    {
                        owner.fallback.Add((renderer, renderer.enabled));
                        // Reuse the authored saddle to connect the complete
                        // native casting to the extended articulated frame.
                        renderer.enabled = renderer.name == "Cylinder saddle";
                    }
                }
                owner.audio = S060NativeAudio.Follow(parts.Car,
                    new Vector3(0, 0, S060MeyerLayout.FrontCylinderZ - S060PipeGeometry.NativeCylinderCentreZ), front);
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        public void Dispose()
        {
            audio?.Dispose(); audio = null;
            foreach (var home in homes)
                if (home.part != null)
                {
                    home.part.SetParent(home.parent, false); home.part.localPosition = home.position;
                    home.part.localRotation = home.rotation; home.part.localScale = home.scale;
                }
            homes.Clear();
            foreach (var home in fallback) if (home.renderer != null) home.renderer.enabled = home.enabled;
            fallback.Clear();
        }
    }
}
#endif
