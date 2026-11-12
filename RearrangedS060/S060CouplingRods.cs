using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // Exactly the live wheel bosses: remove the dropped centre boss on 0-4-0
    // and duplicated overlapping three-pin rods on 0-8-0. Only meshes change.
    internal sealed class S060CouplingRods : IDisposable
    {
        private readonly List<S060RodMeshSwap> swaps = new List<S060RodMeshSwap>();
        internal static S060CouplingRods Apply(S060Parts parts)
        {
            var owner = new S060CouplingRods();
            try
            {
                foreach (var side in new[] { parts.MechanismLeft, parts.MechanismRight })
                {
                    var wheels = side.Cast<Transform>().Where(t => t.name.StartsWith("s060_Wheels_0", StringComparison.Ordinal) && !t.name.Contains("LOD") && t.gameObject.activeInHierarchy).OrderBy(t => t.localPosition.z).ToArray();
                    if (wheels.Length != 2 && wheels.Length != 4 && wheels.Length != 5) throw new InvalidOperationException("Fitted coupling rods require two, four or five driven axles.");
                    var rod = side.Find("s060_mech_wheels_connect") ?? throw new InvalidOperationException("Native coupling-rod animation parent missing.");
                    foreach (var filter in rod.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var pins = wheels.Select(w => filter.transform.InverseTransformPoint(w.TransformPoint(new Vector3(.98730177f, -S060OilCupGeometry.CrankRadius, 0))))
                            .Select(p => new Vector2(p.y, p.z)).ToArray();
                        owner.swaps.Add(new S060RodMeshSwap(filter, pins));
                    }
                }
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        public void Dispose() { foreach (var swap in swaps) swap.Dispose(); swaps.Clear(); }
    }
}
