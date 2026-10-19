#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS282
{
    // Register presentation with DV's existing burn/repair lifecycle. B99's S282
    // swaps materials, not its drivetrain prefab. No cloned exploded locomotive,
    // extra animation/physics controller or global explosion patch is required.
    internal sealed class B99ExplosionMaterials
    {
        private readonly ExplosionModelHandler handler;
        private readonly HashSet<Renderer> originals;
        private readonly ExplosionModelHandler.MaterialSwapData added;
        internal int Count => added.affectedRenderers.Count;

        internal B99ExplosionMaterials(TrainCar car, Transform nativeDrivetrain)
        {
            handler = car.GetComponent<ExplosionModelHandler>();
            originals = new HashSet<Renderer>(car.GetComponentsInChildren<Renderer>(true));
            var reference = nativeDrivetrain.GetComponentInChildren<MeshRenderer>(true);
            var body = handler?.materialSwaps?.FirstOrDefault(s => s != null && s.swapMaterial != null &&
                (s.affectedGameObjects ?? Array.Empty<GameObject>()).Any(root => root != null &&
                    reference != null && reference.transform.IsChildOf(root.transform)));
            if (body == null)
                throw new InvalidOperationException("Native S282 drivetrain explosion material was not found.");
            added = new ExplosionModelHandler.MaterialSwapData
            {
                swapMaterial = body.swapMaterial,
                affectedGameObjects = Array.Empty<GameObject>(),
                // Populate explicitly: native lazy discovery skips inactive LODs
                // and never discovers geometry added after the first explosion.
                affectedRenderers = (body.affectedGameObjects ?? Array.Empty<GameObject>())
                    .Where(root => root != null).SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true))
                    .Distinct().Select(r => ((Renderer)r, r.sharedMaterial)).ToList()
            };
            // Seed inactive original gear too: an initial explosion as a 4-0-4
            // hides every driver, so native discovery would miss those renderers
            // forever when a powered arrangement is selected after repair.
            handler.materialSwaps = handler.materialSwaps.Concat(new[] { added }).ToArray();
        }

        internal void Refresh(IEnumerable<Renderer> renderers)
        {
            if (handler.usingExplodedModel)
                throw new InvalidOperationException("Repair the locomotive before changing its running gear.");
            var registered = new HashSet<Renderer>(added.affectedRenderers.Select(p => p.renderer));
            foreach (var renderer in renderers)
            {
                if (renderer == null || originals.Contains(renderer) || !registered.Add(renderer)) continue;
                added.affectedRenderers.Add((renderer, renderer.sharedMaterial));
            }
        }
    }
}
#endif
