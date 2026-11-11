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
            internal int Side;
            internal bool RetainedChest;
            internal Vector3 AdmissionAxis;
            internal Transform Part, Upper; internal MeshFilter Filter;
            internal Mesh Original, Owned; internal Vector3[] Source, Work;
            internal float[] Weights; internal Vector3 Mount, Previous; internal Quaternion PreviousRotation;
            internal float MaxWeight, WeightedRadius; internal bool Initialized;
            internal Vector3[] NativeTop, NativeCut; internal Mesh Sleeve;
        }
        private sealed class Span
        {
            internal int Side;
            internal Transform Part, From, To; internal MeshFilter Filter;
            internal Mesh Original, Owned; internal Vector3[] Source, Work;
            internal Vector3 Position, Scale; internal Quaternion Rotation;
            internal float Previous = -1;
        }
        private Transform body;
        private readonly List<Manifold> fronts = new List<Manifold>();
        private readonly List<Span> spans = new List<Span>();
        private S060BodyPipes oldBranches;
        private S060MeyerExhaustPipe meyerExhaust;
        private S060MeyerInwardPipes inwardPipes;
        internal S060MeyerExhaustPipe ExhaustReturn => meyerExhaust;
        internal S060MeyerInwardPipes InwardPipes => inwardPipes;
        internal float BoilerSocketRadius(int side) => oldBranches.BoilerSocketRadius(side);
        private readonly List<(GameObject part, Mesh mesh)> boilerSleeves = new List<(GameObject, Mesh)>();
#if UNITY_EDITOR
        private Material sleevePreviewMaterial;
#endif
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
                bool inward = S060MeyerInwardPipes.IsInward(body, front);
                bool dockside = front.Find("Franklin fit R") != null;
                foreach (int side in new[] { -1, 1 })
                {
                    string suffix = side < 0 ? "L" : "R";
                    string frontSuffix = inward ? (side < 0 ? "R" : "L") : suffix;
                    var f = front.Find("[S060 manifold " + frontSuffix + "]"); var r = rear.Find("[S060 manifold " + suffix + "]");
                    var p = front.Find("[S060 transfer pipe " + frontSuffix + "]");
                    if (f == null || r == null || p == null) throw new InvalidOperationException("Update Meyer/Dockside steam-pipe assets to 0.4.0.");
                    var mf = f.GetComponent<MeshFilter>();
                    if (mf.sharedMesh == null || !mf.sharedMesh.isReadable || !p.GetComponent<MeshFilter>().sharedMesh.isReadable)
                        throw new InvalidOperationException("Steam-pipe assets need readable geometry; update both packs to 0.4.0.");
                    var source = mf.sharedMesh.vertices;
                    var m = new Manifold { Part = f, Upper = f.Find("[boiler datum]"), Filter = mf, Original = mf.sharedMesh,
                        Source = source, Work = new Vector3[source.Length], Mount = S060PipeGeometry.BoilerSocket(side), Side = side };
                    float lower = f.Find("[lower datum]").localPosition.y;
                    if (inward)
                    {
                        // Keep the complete chest/transfer flange, but retire
                        // the S282 upper course now replaced by one low feed.
                        m.Owned = S060MeyerInwardPipes.TrimUpperCourse(m.Original, lower + .33f, out m.AdmissionAxis);
                        m.RetainedChest = true;
                    }
                    else
                    {
                        m.Weights = source.Select(v => S060PipeGeometry.FrontWeight(v.y, lower)).ToArray();
                        for (int i = 0; i < source.Length; i++)
                        {
                            m.MaxWeight = Mathf.Max(m.MaxWeight, m.Weights[i]);
                            m.WeightedRadius = Mathf.Max(m.WeightedRadius, m.Weights[i] * (source[i] - m.Upper.localPosition).magnitude);
                        }
                        m.Owned = UnityEngine.Object.Instantiate(m.Original); m.Owned.MarkDynamic();
                    }
                    owner.fronts.Add(m); mf.sharedMesh = m.Owned;
                    var filter = p.GetComponent<MeshFilter>();
                    var span = new Span { Part = p, From = r.Find("[transfer socket]"), To = f.Find("[transfer socket]"), Filter = filter,
                        Original = filter.sharedMesh, Source = filter.sharedMesh.vertices, Position = p.localPosition, Rotation = p.localRotation, Scale = p.localScale, Side = side };
                    span.Work = new Vector3[span.Source.Length]; span.Owned = UnityEngine.Object.Instantiate(span.Original); span.Owned.MarkDynamic();
                    owner.spans.Add(span); filter.sharedMesh = span.Owned; p.localScale = Vector3.one;
                }
                if (nativeShell != null)
                {
                    owner.oldBranches = S060BodyPipes.Apply(nativeShell, Vector3.zero, true, !dockside);
                    foreach (var manifold in owner.fronts)
                    {
                        int side = manifold.Side;
                        manifold.Mount = body.InverseTransformPoint(nativeShell.TransformPoint(owner.oldBranches.BoilerSocket(side)));
                    }
                    var live = nativeShell.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name == "s060_body")?.sharedMaterial;
                    if (dockside) owner.AddBoilerSleeves(live ?? front.GetComponentInChildren<MeshRenderer>().sharedMaterial, nativeShell);
                    if (live != null)
                        foreach (var renderer in front.GetComponentsInChildren<MeshRenderer>(true).Concat(rear.GetComponentsInChildren<MeshRenderer>(true)))
                            if (renderer.sharedMaterial != null && renderer.sharedMaterial.name.StartsWith("LocoS060", StringComparison.Ordinal)) renderer.sharedMaterial = live;
                }
                if (inward)
                {
                    owner.inwardPipes = S060MeyerInwardPipes.Bind(body, front, rear, nativeShell, owner.fronts[0].Mount, owner.fronts[1].Mount,
                        front.InverseTransformPoint(owner.fronts[0].Part.TransformPoint(owner.fronts[0].AdmissionAxis)),
                        front.InverseTransformPoint(owner.fronts[1].Part.TransformPoint(owner.fronts[1].AdmissionAxis)));
                    foreach (var span in owner.spans)
                    { span.From = owner.inwardPipes.RearTransfer(span.Side); span.To = owner.inwardPipes.FrontTransfer(span.Side); }
                }
                if (front.Find("[Native S060 cylinders]") != null)
                    owner.meyerExhaust = S060MeyerExhaustPipe.Bind(body, rear, nativeShell);
                owner.UpdateGeometry(); return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        private void AddBoilerSleeves(Material template, Transform nativeShell)
        {
#if UNITY_EDITOR
            sleevePreviewMaterial = new Material(Shader.Find("Standard")) { color = new Color(.21f, .215f, .22f) };
            sleevePreviewMaterial.SetFloat("_Metallic", .55f); sleevePreviewMaterial.SetFloat("_Glossiness", .28f);
            var material = sleevePreviewMaterial;
#else
            var material = S060ShayFinish.Gunmetal(template);
#endif
            foreach (var manifold in fronts)
            {
                manifold.NativeTop = oldBranches.BoilerSection(manifold.Side, S060PipeGeometry.BoilerCutY + .012f)
                    .Select(p => body.InverseTransformPoint(nativeShell.TransformPoint(p))).ToArray();
                manifold.NativeCut = oldBranches.BoilerSection(manifold.Side, S060PipeGeometry.BoilerCutY)
                    .Select(p => body.InverseTransformPoint(nativeShell.TransformPoint(p))).ToArray();
                var mesh = manifold.Sleeve = new Mesh(); mesh.MarkDynamic();
                mesh.name = "Dockside fitted hollow smokebox sleeve";
                var part = new GameObject("[Dockside smokebox sleeve " + (manifold.Side < 0 ? "L" : "R") + "]");
                boilerSleeves.Add((part, mesh)); part.transform.SetParent(body, false);
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                part.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
        }
        internal void UpdateGeometry()
        {
            if (disposed || body == null) return;
            meyerExhaust?.UpdateGeometry();
            inwardPipes?.UpdateGeometry();
            foreach (var m in fronts)
            {
                if (m.RetainedChest || m.Part == null || m.Upper == null || m.Owned == null) continue;
                Vector3 delta = m.Part.InverseTransformPoint(body.TransformPoint(m.Mount)) - m.Upper.localPosition;
                var from = m.Upper.localRotation * Vector3.up; var to = m.Part.InverseTransformDirection(body.up);
                if (!S060PipePose.Finite(delta) || !S060PipePose.Direction(from) || !S060PipePose.Direction(to)) continue;
                var rotation = Quaternion.FromToRotation(from, to);
                if (!S060PipePose.Refit(m.Initialized, m.Previous, m.PreviousRotation, delta, rotation, m.MaxWeight, m.WeightedRadius)) continue;
                for (int i = 0; i < m.Work.Length; i++)
                {
                    var rigid = rotation * (m.Source[i] - m.Upper.localPosition) + m.Upper.localPosition + delta;
                    m.Work[i] = Vector3.LerpUnclamped(m.Source[i], rigid, m.Weights[i]);
                }
                m.Owned.vertices = m.Work; m.Owned.RecalculateBounds(); m.Owned.RecalculateNormals();
                m.Previous = delta; m.PreviousRotation = rotation; m.Initialized = true;
                if (m.Sleeve != null)
                {
                    var matrix = body.worldToLocalMatrix * m.Part.localToWorldMatrix;
                    var fitted = m.Work.Select(matrix.MultiplyPoint3x4).ToArray(); var triangles = m.Owned.triangles;
                    S060SteamSleeve.Fit(m.Sleeve, m.NativeTop, m.NativeCut,
                        S060SteamSleeve.Section(fitted, triangles, m.Mount.y-.095f), S060SteamSleeve.Section(fitted, triangles, m.Mount.y-.110f));
                }
            }
            foreach (var s in spans)
            {
                if (s.Part == null || s.From == null || s.To == null || s.Owned == null) continue;
                Vector3 a = s.From.position;
                // Derail/rerail and world moves can temporarily invalidate a
                // visual span. Keep its last good mesh/pose and retry next frame,
                // as the LP exhaust return does; never feed NaN into Unity or
                // throw the same overlap exception on every LateUpdate.
                if (!S060PipePose.Span(a, s.To.position, body.up, out var line, out float length)) continue;
                s.Part.SetPositionAndRotation(a, Quaternion.LookRotation(line, body.up));
                if (Math.Abs(length - s.Previous) < S060PipePose.MaxError) continue;
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
            meyerExhaust?.Dispose(); meyerExhaust = null;
            inwardPipes?.Dispose(); inwardPipes = null;
            foreach (var m in fronts) { if (m.Filter != null) m.Filter.sharedMesh = m.Original; if (m.Owned != null) Release(m.Owned); }
            foreach (var s in spans)
            {
                if (s.Filter != null) s.Filter.sharedMesh = s.Original;
                if (s.Part != null) { s.Part.localPosition = s.Position; s.Part.localRotation = s.Rotation; s.Part.localScale = s.Scale; }
                if (s.Owned != null) Release(s.Owned);
            }
            foreach (var sleeve in boilerSleeves)
            {
                if (sleeve.part != null) { sleeve.part.SetActive(false); Release(sleeve.part); }
                if (sleeve.mesh != null) Release(sleeve.mesh);
            }
            boilerSleeves.Clear();
#if UNITY_EDITOR
            if (sleevePreviewMaterial != null) Release(sleevePreviewMaterial);
#endif
            fronts.Clear(); spans.Clear(); oldBranches?.Dispose(); oldBranches = null;
#if !UNITY_EDITOR
            if (!destroying) Release(this);
#endif
        }
        private static void Release(UnityEngine.Object value) { if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
        private void OnDestroy() { destroying = true; if (!disposed) Dispose(); }
    }

    internal static class S060PipePose
    {
        // Give each adjoining fit half a .25 mm joint budget, below the authored
        // .3 mm cap-fit tolerance even if both errors add. The bound compares
        // against the LAST UPLOADED mesh, so skipped suspension jitter cannot
        // accumulate into a detached pipe. No normal rebuild is needed while
        // every weighted vertex remains within its eighth-millimetre budget.
        internal const float MaxError = .000125f;
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        internal static bool Direction(Vector3 value) => Finite(value) && Finite(value.sqrMagnitude) && value.sqrMagnitude > 1e-12f;
        private static double Norm(Quaternion q) => (double)q.x * q.x + (double)q.y * q.y + (double)q.z * q.z + (double)q.w * q.w;
        private static bool Rotation(Quaternion q) => Finite(q.x) && Finite(q.y) && Finite(q.z) && Finite(q.w) && Norm(q) > 1e-12;

        internal static bool Span(Vector3 from, Vector3 to, Vector3 up, out Vector3 line, out float length)
        {
            line = Vector3.zero; length = 0;
            if (!Finite(from) || !Finite(to) || !Direction(up)) return false;
            line = to - from; length = line.magnitude;
            if (!Finite(line) || !Finite(length) || length < .5f) return false;
            // LookRotation needs a usable forward/up frame as well as distance.
            return Direction(Vector3.Cross(line.normalized, up.normalized));
        }

        internal static bool Refit(bool initialized, Vector3 previous, Quaternion previousRotation, Vector3 delta, Quaternion rotation, float maxWeight, float weightedRadius)
        {
            if (!Finite(delta) || !Rotation(rotation) || !Finite(maxWeight) || !Finite(weightedRadius) || maxWeight < 0 || weightedRadius < 0) return false;
            if (!initialized) return true;
            if (delta.Equals(previous) && rotation.Equals(previousRotation)) return false;
            double dx = (double)delta.x - previous.x, dy = (double)delta.y - previous.y, dz = (double)delta.z - previous.z;
            double a = Math.Sqrt(Norm(rotation)), b = Math.Sqrt(Norm(previousRotation));
            // q and -q express the same rotation. A normalized quaternion chord
            // bounds the vertex's rotational displacement by 2*r*|q1-q0|.
            // Double arithmetic avoids Quaternion.Angle's loss of resolution
            // around zero, where this small fit budget matters most.
            double minus = 0, plus = 0;
            for (int i = 0; i < 4; i++)
            {
                double x = rotation[i] / a, y = previousRotation[i] / b;
                minus += (x - y) * (x - y); plus += (x + y) * (x + y);
            }
            return maxWeight * Math.Sqrt(dx * dx + dy * dy + dz * dz) + 2 * weightedRadius * Math.Sqrt(Math.Min(minus, plus)) >= MaxError;
        }
    }
}
