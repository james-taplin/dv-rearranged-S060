using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Front-only mechanical fit after native absolute-time animation. The real
    // animated wheel pin supplies the pose; this never integrates RPM or changes
    // native crank/valve/save ports. Oil cups follow after this pass.
    [DefaultExecutionOrder(1500)]
    internal sealed class S060MeyerFrontMotion :
#if !UNITY_EDITOR
        MonoBehaviour,
#endif
        IDisposable
    {
        private sealed class Side
        {
            internal int Sign;
            internal Transform Wheel, Main, Crosshead, Piston;
            internal MeshFilter Filter;
            internal Mesh Source, Owned;
        }
        private readonly List<Side> sides = new List<Side>();
        private readonly List<(Transform part, Vector3 position, Quaternion rotation, Vector3 scale)> homes = new List<(Transform, Vector3, Quaternion, Vector3)>();
        private Transform frame;
        private bool disposed;
#if !UNITY_EDITOR
        private bool destroying;
#endif
        internal static S060MeyerFrontMotion Bind(Transform front)
        {
#if UNITY_EDITOR
            var owner = new S060MeyerFrontMotion();
#else
            var owner = front.gameObject.AddComponent<S060MeyerFrontMotion>();
#endif
            owner.frame = front;
            try
            {
                foreach (int sign in new[] { -1, 1 })
                {
                    string suffix = sign < 0 ? "L" : "R";
                    var rods = front.Find("[Meyer rods " + suffix + "]") ?? throw new InvalidOperationException("Meyer front rods missing.");
                    var side = new Side { Sign = sign,
                        Wheel = front.Find("[axle] rear/S060 driver " + suffix + " (native alias)"),
                        Main = rods.Find("Main rod"), Crosshead = rods.Find("Crosshead"), Piston = rods.Find("Piston rod") };
                    if (side.Wheel == null || side.Main == null || side.Crosshead == null || side.Piston == null) throw new InvalidOperationException("Complete Meyer front mechanism required.");
                    foreach (var part in new[] { side.Main, side.Crosshead, side.Piston }) owner.Remember(part);
                    side.Filter = side.Main.GetComponent<MeshFilter>(); side.Source = side.Filter.sharedMesh;
                    side.Owned = S060RodMesh.Normalized("Meyer inward front main rod", S060MeyerLayout.FrontMainRodLength, .085f, .0595f);
                    owner.sides.Add(side); side.Filter.sharedMesh = side.Owned;
                    foreach (var part in rods.Cast<Transform>().Where(t => t.name.StartsWith("Crosshead guide", StringComparison.Ordinal) || t.name == "Guide end cheek" || t.name == "Guide lower tie")) owner.Translate(part);
                    owner.Translate(front.Find("[Round cylinders " + suffix + "]"));
                    owner.Translate(front.Find("[S060 manifold " + suffix + "]"));
                }
                // Emitters and hiss holders are siblings of the casting, not
                // children of its mesh. Move their existing native readers too.
                var effects = front.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Cock front ", StringComparison.Ordinal) || t.name.StartsWith("Cock rear ", StringComparison.Ordinal) ||
                    t.name.StartsWith("Water drip ", StringComparison.Ordinal) || t.name.StartsWith("Cock audio ", StringComparison.Ordinal)).ToArray();
                foreach (var part in effects.Where(t => !effects.Any(other => other != t && t.IsChildOf(other)))) owner.Translate(part);
                // Keep frame/saddle connections: extend existing rails toward
                // the moved cylinders while preserving their opposite endpoints.
                var structure = front.Find("[Powered bogie frame]");
                if (structure == null) throw new InvalidOperationException("Meyer powered frame missing.");
                foreach (var rail in structure.Cast<Transform>().Where(t => t.name.StartsWith("Main frame rail ", StringComparison.Ordinal) || t.name.StartsWith("Visible upper frame edge ", StringComparison.Ordinal)))
                {
                    owner.Remember(rail); float rear = rail.localPosition.z - rail.localScale.z / 2;
                    float forward = rail.name.StartsWith("Main frame rail ", StringComparison.Ordinal) ? S060MeyerLayout.FrontCylinderZ + .24f : .76f + S060MeyerLayout.FrontCylinderShift;
                    var p = rail.localPosition; p.z = (rear + forward) / 2; rail.localPosition = p;
                    var scale = rail.localScale; scale.z = forward - rear; rail.localScale = scale;
                }
                owner.ApplyPose(); return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        private void Remember(Transform part)
        { if (part == null) throw new InvalidOperationException("Meyer front fitting datum missing."); if (!homes.Any(h => h.part == part)) homes.Add((part, part.localPosition, part.localRotation, part.localScale)); }
        private void Translate(Transform part)
        { Remember(part); part.localPosition += Vector3.forward * S060MeyerLayout.FrontCylinderShift; }
        internal void ApplyPose()
        {
            if (disposed || frame == null) return;
            foreach (var side in sides)
            {
                if (side.Wheel == null) continue;
                var pin = frame.InverseTransformPoint(S060OilCupGeometry.Pin(frame, side.Wheel, side.Sign * S060OilCupGeometry.MainRodPlaneX));
                float dy = pin.y - (float)MeyerPlan.DriverRadius, length = S060MeyerLayout.FrontMainRodLength;
                var crosshead = new Vector3(pin.x, (float)MeyerPlan.DriverRadius, pin.z + Mathf.Sqrt(Mathf.Max(0, length * length - dy * dy)));
                side.Crosshead.localPosition = crosshead;
                var line = crosshead - pin; side.Main.localPosition = (pin + crosshead) / 2;
                side.Main.localRotation = Quaternion.LookRotation(line, Vector3.up); side.Main.localScale = new Vector3(1, 1, length);
                var piston = crosshead; piston.x = side.Sign * S060OilCupGeometry.PistonAxisX;
                var gland = new Vector3(piston.x, (float)MeyerPlan.DriverRadius, S060MeyerLayout.FrontCylinderZ + .05f);
                side.Piston.localPosition = (piston + gland) / 2;
                side.Piston.localRotation = Quaternion.identity; side.Piston.localScale = new Vector3(.035f, .035f, gland.z - piston.z);
            }
        }
        private void LateUpdate() => ApplyPose();
        public void Dispose()
        {
            if (disposed) return; disposed = true;
#if !UNITY_EDITOR
            enabled = false;
#endif
            foreach (var side in sides)
            { if (side.Filter != null && side.Filter.sharedMesh == side.Owned) side.Filter.sharedMesh = side.Source; if (side.Owned != null) Release(side.Owned); }
            sides.Clear();
            foreach (var home in homes) if (home.part != null) { home.part.localPosition = home.position; home.part.localRotation = home.rotation; home.part.localScale = home.scale; }
            homes.Clear(); frame = null;
#if !UNITY_EDITOR
            if (!destroying) Release(this);
#endif
        }
#if !UNITY_EDITOR
        private void OnDestroy() { destroying = true; Dispose(); }
#endif
        private static void Release(Object item) { if (Application.isPlaying) Object.Destroy(item); else Object.DestroyImmediate(item); }
    }
}
