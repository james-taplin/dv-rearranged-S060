#if B99_DOCKSIDE
using System;
using HarmonyLib;
using UnityEngine;

namespace RearrangedS060
{
    internal static class DocksideBody
    {
        internal static void Prepare(TrainCar car)
        {
            var model = car.transform.Find("Model");
            if (model == null) throw new InvalidOperationException("Dockside model root missing.");
            if (model.Find("[Dockside split S060 shell]") != null) return;
            var original = model.Find("S060 body (native alias)")?.GetComponent<MeshRenderer>();
            if (original == null || original.sharedMaterial == null) throw new InvalidOperationException("Dockside native body/material alias unresolved.");
            // The existing splitter already separates the baked stock cylinders
            // and three-axle brake calipers from the body. Reuse its immutable
            // cached geometry, rather than drawing that stock gear over our units
            // or inventing another mesh-cutting pipeline.
            var type = AccessTools.TypeByName("LocoMeshSplitter.MeshSplitters.S060.S060LOD0MeshSplitter");
            var source = type == null ? null : AccessTools.Property(type, "SplitLocoBodyLOD0")?.GetValue(null) as GameObject;
            if (source == null) throw new InvalidOperationException("LocoMeshSplitter S060 shell cache is not ready.");
            var copy = UnityEngine.Object.Instantiate(source, model); copy.name = "[Dockside split S060 shell]";
            copy.transform.localPosition = Vector3.zero; copy.transform.localRotation = Quaternion.identity; copy.transform.localScale = Vector3.one;
            try
            {
                foreach (string name in new[] { "s060_cylinder_left", "s060_cylinder_right", "s060_caliper_l_1", "s060_caliper_l_2", "s060_caliper_l_3", "s060_caliper_r_1", "s060_caliper_r_2", "s060_caliper_r_3" })
                {
                    var part = copy.transform.Find(name);
                    if (part == null) throw new InvalidOperationException("Split S060 shell part missing: " + name);
                    part.gameObject.SetActive(false);
                }
                foreach (var renderer in copy.GetComponentsInChildren<MeshRenderer>(true)) renderer.sharedMaterial = original.sharedMaterial;
                foreach (var renderer in copy.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.name.IndexOf("brake_shoes", StringComparison.OrdinalIgnoreCase) >= 0) renderer.enabled = false;
                copy.SetActive(true); original.enabled = false;
            }
            catch { copy.SetActive(false); UnityEngine.Object.Destroy(copy); throw; }
        }
    }
}
#endif
