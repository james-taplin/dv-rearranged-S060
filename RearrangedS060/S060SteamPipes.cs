using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // Uses the S282's existing custom manifolds/dry pipe. Only the moving
    // connection fit is ours; the simulation's native/compound plumbing stays.
    internal sealed class S060SteamPipes :
#if !UNITY_EDITOR
        MonoBehaviour,
#endif
        IDisposable
    {
        private sealed class Manifold
        {
            internal Transform Part, Upper; internal MeshFilter Filter;
            internal Mesh Original, Owned; internal Vector3[] Source, Work;
            internal float[] Weights; internal Vector3 Mount, Previous; internal Quaternion PreviousRotation;
        }
        private sealed class Span
        {
            internal Transform Part, From, To; internal MeshFilter Filter;
            internal Mesh Original, Owned; internal Vector3[] Source, Work;
            internal Vector3 Position, Scale; internal Quaternion Rotation;
            internal float Previous = -1;
        }
        private Transform body;
        private readonly List<Manifold> fronts = new List<Manifold>();
        private readonly List<Span> spans = new List<Span>();
        private S060BodyPipes oldBranches;
        private bool disposed, destroying;
        internal static S060SteamPipes Bind(Transform body, Transform front, Transform rear, Transform nativeShell)
        {
#if UNITY_EDITOR
            // Asset validation exercises the same fitting code without trying
            // to attach an Editor-assembly script to a preview GameObject.
            var owner = new S060SteamPipes();
#else
            var owner = front.gameObject.AddComponent<S060SteamPipes>();
#endif
            owner.body = body;
            try
            {
                foreach (int side in new[] { -1, 1 })
                {
                    string suffix = side < 0 ? "L" : "R";
                    var f = front.Find("[S060 manifold " + suffix + "]"); var r = rear.Find("[S060 manifold " + suffix + "]");
                    var p = front.Find("[S060 transfer pipe " + suffix + "]");
                    if (f == null || r == null || p == null) throw new InvalidOperationException("Update Meyer/Dockside steam-pipe assets to 0.4.0.");
                    var mf = f.GetComponent<MeshFilter>();
                    if (mf.sharedMesh == null || !mf.sharedMesh.isReadable || !p.GetComponent<MeshFilter>().sharedMesh.isReadable)
                        throw new InvalidOperationException("Steam-pipe assets need readable geometry; update both packs to 0.4.0.");
                    var source = mf.sharedMesh.vertices;
                    var m = new Manifold { Part = f, Upper = f.Find("[boiler datum]"), Filter = mf, Original = mf.sharedMesh,
                        Source = source, Work = new Vector3[source.Length], Mount = S060PipeGeometry.BoilerSocket(side), Previous = Vector3.one * float.MaxValue };
                    float lower = f.Find("[lower datum]").localPosition.y;
                    m.Weights = source.Select(v => S060PipeGeometry.FrontWeight(v.y, lower)).ToArray();
                    m.Owned = UnityEngine.Object.Instantiate(m.Original); m.Owned.MarkDynamic(); owner.fronts.Add(m); mf.sharedMesh = m.Owned;
                    var filter = p.GetComponent<MeshFilter>();
                    var span = new Span { Part = p, From = r.Find("[transfer socket]"), To = f.Find("[transfer socket]"), Filter = filter,
                        Original = filter.sharedMesh, Source = filter.sharedMesh.vertices, Position = p.localPosition, Rotation = p.localRotation, Scale = p.localScale };
                    span.Work = new Vector3[span.Source.Length]; span.Owned = UnityEngine.Object.Instantiate(span.Original); span.Owned.MarkDynamic();
                    owner.spans.Add(span); filter.sharedMesh = span.Owned; p.localScale = Vector3.one;
                }
                if (nativeShell != null)
                {
                    owner.oldBranches = S060BodyPipes.Apply(nativeShell, Vector3.zero, true);
                    foreach (var manifold in owner.fronts)
                    {
                        int side = manifold.Part.name.EndsWith("L]", StringComparison.Ordinal) ? -1 : 1;
                        manifold.Mount = body.InverseTransformPoint(nativeShell.TransformPoint(owner.oldBranches.BoilerSocket(side)));
                    }
                    var live = nativeShell.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name == "s060_body")?.sharedMaterial;
                    if (live != null)
                        foreach (var renderer in front.GetComponentsInChildren<MeshRenderer>(true).Concat(rear.GetComponentsInChildren<MeshRenderer>(true)))
                            if (renderer.sharedMaterial != null && renderer.sharedMaterial.name.StartsWith("LocoS060", StringComparison.Ordinal)) renderer.sharedMaterial = live;
                }
                owner.UpdateGeometry(); return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        internal void UpdateGeometry()
        {
            if (disposed || body == null) return;
            foreach (var m in fronts)
            {
                Vector3 delta = m.Part.InverseTransformPoint(body.TransformPoint(m.Mount)) - m.Upper.localPosition;
                var rotation = Quaternion.FromToRotation(m.Upper.localRotation * Vector3.up, m.Part.InverseTransformDirection(body.up));
                if ((delta - m.Previous).sqrMagnitude < 1e-8f && Quaternion.Angle(rotation, m.PreviousRotation) < .001f) continue;
                for (int i = 0; i < m.Work.Length; i++)
                {
                    var rigid = rotation * (m.Source[i] - m.Upper.localPosition) + m.Upper.localPosition + delta;
                    m.Work[i] = Vector3.LerpUnclamped(m.Source[i], rigid, m.Weights[i]);
                }
                m.Owned.vertices = m.Work; m.Owned.RecalculateBounds(); m.Owned.RecalculateNormals(); m.Previous = delta; m.PreviousRotation = rotation;
            }
            foreach (var s in spans)
            {
                Vector3 a = s.From.position, line = s.To.position - a;
                float length = line.magnitude;
                if (length < .5f) throw new InvalidOperationException("Steam transfer pipe endpoints overlap.");
                s.Part.SetPositionAndRotation(a, Quaternion.LookRotation(line, body.up));
                if (Math.Abs(length - s.Previous) < .0001f) continue;
                for (int i = 0; i < s.Work.Length; i++) { var v = s.Source[i]; v.z = S060PipeGeometry.SpanZ(v.z, length); s.Work[i] = v; }
                s.Owned.vertices = s.Work; s.Owned.RecalculateBounds(); s.Previous = length;
            }
        }
        private void LateUpdate() => UpdateGeometry();
        public void Dispose()
        {
            if (disposed) return; disposed = true;
#if !UNITY_EDITOR
            enabled = false;
#endif
            foreach (var m in fronts) { if (m.Filter != null) m.Filter.sharedMesh = m.Original; if (m.Owned != null) Release(m.Owned); }
            foreach (var s in spans)
            {
                if (s.Filter != null) s.Filter.sharedMesh = s.Original;
                if (s.Part != null) { s.Part.localPosition = s.Position; s.Part.localRotation = s.Rotation; s.Part.localScale = s.Scale; }
                if (s.Owned != null) Release(s.Owned);
            }
            fronts.Clear(); spans.Clear(); oldBranches?.Dispose(); oldBranches = null;
#if !UNITY_EDITOR
            if (!destroying) Release(this);
#endif
        }
        private static void Release(UnityEngine.Object value) { if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
        private void OnDestroy() { destroying = true; if (!disposed) Dispose(); }
    }
}
