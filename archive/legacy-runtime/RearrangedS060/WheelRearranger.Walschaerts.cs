#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    internal partial class WheelRearranger
    {
        private readonly List<PacificHudsonPart> pacificHudsonParts = new List<PacificHudsonPart>();
        private readonly List<PacificHudsonPipe> pacificHudsonPipes = new List<PacificHudsonPipe>();
        private readonly List<Transform> pacificHudsonReachSupports = new List<Transform>();
        private string pacificHudsonPreparationError;
        private Vector3 pacificHudsonCrossheadLScale, pacificHudsonCrossheadRScale;

        // Capture the native poses before Show6Drivers or either geometry snapshot
        // is applied. All targets remain native objects, with their native owners.
        private void SetupPacificHudsonValveGear()
        {
            DisposePacificHudsonValveGear();
            try { CachePacificHudsonValveGear(); }
            catch (Exception ex)
            {
                // This optional polish is prepared for every S282. A missing
                // asset must only reject a later request for this polish.
                pacificHudsonPreparationError = "Pacific/Hudson native geometry could not be prepared: " + ex.Message;
            }
        }

        private void CachePacificHudsonValveGear()
        {
            pacificHudsonParts.Clear();
            pacificHudsonPipes.Clear();
            pacificHudsonReachSupports.Clear();
            pacificHudsonPreparationError = null;
            pacificHudsonCrossheadLScale = lCrosshead.localScale;
            pacificHudsonCrossheadRScale = rCrosshead.localScale;
            foreach (string lod in new[] { "Static_LOD0", "Static_LOD1" })
            {
                var container = transform.Find("LocoS282A_Body/" + lod);
                Transform split = null;
                if (container != null)
                    foreach (Transform child in container)
                        if (child.name.StartsWith("s282a_split_body", StringComparison.Ordinal)) { split = child; break; }
                var reachSupport = split != null ? split.Find("s282a_reach_rod_support") : null;
                if (reachSupport != null) pacificHudsonReachSupports.Add(reachSupport);
                foreach (string name in new[] { "s282a_cylinder_l", "s282a_cylinder_r", "s282a_cylinder_cocks",
                    "s282a_crosshead_guide_l", "s282a_crosshead_guide_r",
                    "s282a_lifting_arm_support_l", "s282a_lifting_arm_support_r" })
                    CachePacificHudsonPart(split != null ? split.Find(name) : null, lod + "/" + name);
                foreach (string name in new[] { "s282a_dry_pipe_l", "s282a_dry_pipe_r" })
                {
                    var part = split != null ? split.Find(name) : null;
                    var filter = part != null ? part.GetComponent<MeshFilter>() : null;
                    if (filter == null || filter.sharedMesh == null)
                        pacificHudsonPreparationError = "Missing Pacific/Hudson native dry pipe: " + lod + "/" + name;
                    else pacificHudsonPipes.Add(new PacificHudsonPipe(filter, driveR));
                }
            }
            // LMS only translates these holders. Move their outlets with the
            // cylinders but never stretch emitter shapes, readers or ports.
            foreach (string path in new[] { "LocoS282A_Particles/CylCockSteam", "LocoS282A_Particles/CylCockWaterDrip",
                "LocoS282A_Particles/CylinderCrack" })
                CachePacificHudsonPart(transform.Find(path), path, true);
            // Native item/walk cylinder shells follow the visible cylinder. The
            // fused boiler/body collision mesh and all rigidbodies stay untouched.
            foreach (string path in new[] { "[colliders]/[walkable]/[old]/Exterior/cylinder",
                "[colliders]/[walkable]/[old]/Exterior/cylinder (1)",
                "[colliders]/[walkable]/[old]/Exterior/area_big_cylinder",
                "[colliders]/[items]/[old]/Exterior/area_big_cylinder" })
            {
                var part = transform.Find(path);
                if (part != null) CachePacificHudsonPart(part, path);
            }
        }

        private void CachePacificHudsonPart(Transform part, string path, bool positionOnly = false)
        {
            // A missing optional visual must not prevent preparation of unrelated
            // arrangements. Reject only a requested polished arrangement instead.
            if (part == null) { pacificHudsonPreparationError = "Missing Pacific/Hudson native part: " + path; return; }
            pacificHudsonParts.Add(new PacificHudsonPart(part, driveR, path, positionOnly));
        }

        private void ApplyPacificHudsonValveGear()
        {
            if (pacificHudsonPreparationError != null) throw new InvalidOperationException(pacificHudsonPreparationError);
            if (pacificHudsonParts.Count == 0 || pacificHudsonPipes.Count != 4)
                throw new InvalidOperationException("Pacific/Hudson native geometry was not prepared.");
            // Show6Drivers already applies one uniform Y/Z mapping to every
            // animated linkage. Apply that identical mapping to the static mating
            // parts, rather than overriding the Animator's joint curves each frame.
            foreach (var part in pacificHudsonParts) part.Apply(driveR);
            foreach (var pipe in pacificHudsonPipes) pipe.Apply(driveR);
            ShowValveGear();
            lLiftingArmSupport.gameObject.SetActive(true);
            rLiftingArmSupport.gameObject.SetActive(true);
            // These terminate at the unscaled cab/oiler, not another member of
            // this mechanism. Keep the legacy exclusions instead of floating ends.
            HideReachRod();
            foreach (var support in pacificHudsonReachSupports)
                if (support != null) support.gameObject.SetActive(false);
            driveR.Find("s282_mech_cutoff_rail/s282_mech_lubricator_rod").gameObject.SetActive(false);
            driveR.Find("LubricatorParts").gameObject.SetActive(false);
            // The native clips bind crosshead scale as a constant. Match that
            // authored value immediately; subsequent native samples preserve it.
            lCrosshead.localScale = pacificHudsonCrossheadLScale;
            rCrosshead.localScale = pacificHudsonCrossheadRScale;
        }

        private void DisposePacificHudsonValveGear()
        {
            foreach (var pipe in pacificHudsonPipes) pipe.Dispose();
            pacificHudsonPipes.Clear();
            pacificHudsonParts.Clear();
            pacificHudsonReachSupports.Clear();
        }

        private sealed class PacificHudsonPart
        {
            private readonly Transform part;
            private readonly Matrix4x4 frameInNativeDrive;
            private readonly Vector3 nativeScale;
            private readonly Quaternion nativeRotation;
            private readonly Vector3 emitterCentre;
            private readonly string path;
            private readonly bool positionOnly;
            private bool shearLogged;
            internal PacificHudsonPart(Transform part, Transform nativeDrive, string path, bool positionOnly)
            {
                this.part = part; this.path = path; this.positionOnly = positionOnly;
                frameInNativeDrive = nativeDrive.worldToLocalMatrix * part.localToWorldMatrix;
                nativeScale = part.localScale;
                nativeRotation = part.localRotation;
                emitterCentre = positionOnly ? PacificHudsonAffine.EmitterCentreLocal(part) : Vector3.zero;
            }
            internal void Apply(Transform drive)
            {
                if (part == null) throw new InvalidOperationException("Pacific/Hudson native part was destroyed.");
                var local = part.parent.worldToLocalMatrix * drive.localToWorldMatrix * frameInNativeDrive;
                if (!positionOnly &&
                    PacificHudsonAffine.TryDecompose(local, nativeScale, out var position, out var rotation, out var scale))
                {
                    part.localPosition = position; part.localRotation = rotation; part.localScale = scale;
                    return;
                }
                // The six-driver Y/Z stretch skews any part rotated off the drive's
                // X axis. Such a part follows the mapped pivot with its native
                // orientation and size instead of rejecting the whole conversion.
                if (!positionOnly && !shearLogged)
                {
                    shearLogged = true;
                    Main.Logger.Log("Pacific/Hudson: " + path + " cannot be stretched without shear; moved only.");
                }
                // Move so the emitter centre lands on its mapped position, keeping
                // the holder's native orientation and size.
                part.localPosition = local.MultiplyPoint3x4(emitterCentre) - Matrix4x4.TRS(Vector3.zero, nativeRotation, nativeScale).MultiplyPoint3x4(emitterCentre);
                part.localRotation = nativeRotation; part.localScale = nativeScale;
            }
        }

        private sealed class PacificHudsonPipe
        {
            private readonly MeshFilter filter;
            private readonly Mesh source;
            private readonly Matrix4x4 frameInNativeDrive;
            private Mesh corrected;
            internal PacificHudsonPipe(MeshFilter filter, Transform nativeDrive)
            {
                this.filter = filter; source = filter.sharedMesh;
                frameInNativeDrive = nativeDrive.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            }
            internal void Apply(Transform drive)
            {
                if (filter == null || source == null) throw new InvalidOperationException("Pacific/Hudson native dry pipe was destroyed.");
                if (corrected == null)
                {
                    if (!source.isReadable) throw new InvalidOperationException("Pacific/Hudson native dry pipe mesh is not readable.");
                    var localMapping = filter.transform.worldToLocalMatrix * drive.localToWorldMatrix * frameInNativeDrive;
                    var vertices = PacificHudsonAffine.BendPipe(source.vertices, frameInNativeDrive, localMapping);
                    var copy = Object.Instantiate(source);
                    try
                    {
                        copy.name = "[Rearranged Pacific/Hudson] " + source.name;
                        copy.vertices = vertices; copy.RecalculateBounds(); copy.RecalculateNormals();
                        if (copy.uv.Length == copy.vertexCount) copy.RecalculateTangents();
                        corrected = copy;
                    }
                    catch { Object.Destroy(copy); throw; }
                }
                filter.sharedMesh = corrected;
            }
            internal void Dispose()
            {
                if (filter != null && filter.sharedMesh == corrected) filter.sharedMesh = source;
                if (corrected != null) Object.Destroy(corrected);
                corrected = null;
            }
        }
    }

    // Pure geometry helpers are exercised against the actual source by the
    // offline fixture; they do not sample time or alter simulation/animation.
    internal static class PacificHudsonAffine
    {
        // Cylinder outlet holders (CylCockSteam, CylCockWaterDrip) sit at the car
        // origin with their emitters as children at the cylinders. Mapping the
        // holder's origin moves the jets by the whole engine offset; map the
        // emitters' centre instead. Holder-local; zero when it has no children.
        internal static Vector3 EmitterCentreLocal(Transform holder)
        {
            // Average of the leaf descendants, so intermediate containers at the
            // holder origin do not hide the emitters below them.
            var sum = Vector3.zero; int count = 0;
            void Visit(Transform t)
            {
                bool leaf = true;
                foreach (Transform child in t) { leaf = false; Visit(child); }
                if (leaf && t != holder) { sum += holder.worldToLocalMatrix.MultiplyPoint3x4(t.localToWorldMatrix.MultiplyPoint3x4(Vector3.zero)); count++; }
            }
            Visit(holder);
            return count == 0 ? Vector3.zero : sum / count;
        }

        internal static void Decompose(Matrix4x4 frame, Vector3 nativeScale,
            out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            if (!TryDecompose(frame, nativeScale, out position, out rotation, out scale))
                throw new InvalidOperationException("Pacific/Hudson native support cannot be mapped without shear.");
        }

        // False only for a sheared mapping; singular or nonfinite input still throws.
        internal static bool TryDecompose(Matrix4x4 frame, Vector3 nativeScale,
            out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            rotation = Quaternion.identity;
            position = frame.MultiplyPoint3x4(Vector3.zero);
            var x = frame.MultiplyVector(Vector3.right);
            var y = frame.MultiplyVector(Vector3.up);
            var z = frame.MultiplyVector(Vector3.forward);
            scale = new Vector3(x.magnitude * Mathf.Sign(nativeScale.x), y.magnitude * Mathf.Sign(nativeScale.y), z.magnitude * Mathf.Sign(nativeScale.z));
            if (!Finite(position) || !Finite(scale) || Mathf.Abs(scale.x * scale.y * scale.z) < 1e-8f)
                throw new InvalidOperationException("Pacific/Hudson native support has a singular transform.");
            x /= scale.x; y /= scale.y; z /= scale.z;
            if (Mathf.Abs(Vector3.Dot(x,y)) > 1e-4f || Mathf.Abs(Vector3.Dot(x,z)) > 1e-4f ||
                Mathf.Abs(Vector3.Dot(y,z)) > 1e-4f || Vector3.Dot(Vector3.Cross(x,y),z) < .9999f)
                return false;
            rotation = Quaternion.LookRotation(z,y);
            return true;
        }

        internal static Vector3[] BendPipe(Vector3[] vertices, Matrix4x4 nativeFrame, Matrix4x4 localMapping)
        {
            if (vertices == null || vertices.Length < 3) throw new InvalidOperationException("Pacific/Hudson dry pipe has no usable geometry.");
            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            foreach (var vertex in vertices)
            {
                var point = nativeFrame.MultiplyPoint3x4(vertex);
                if (!Finite(point)) throw new InvalidOperationException("Pacific/Hudson dry pipe contains nonfinite vertices.");
                low = Mathf.Min(low, point.y); high = Mathf.Max(high, point.y);
            }
            if (high - low < .01f) throw new InvalidOperationException("Pacific/Hudson dry pipe has no vertical socket separation.");
            var result = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; ++i)
            {
                var original = vertices[i];
                float weight = Mathf.Clamp01((high - nativeFrame.MultiplyPoint3x4(original).y) / (high - low));
                // Zero blend slope at both ends avoids an abrupt socket kink.
                weight = weight * weight * (3f - 2f * weight);
                var mapped = localMapping.MultiplyPoint3x4(original);
                result[i] = original + (mapped - original) * weight;
                if (!Finite(result[i])) throw new InvalidOperationException("Pacific/Hudson dry-pipe correction is nonfinite.");
            }
            return result;
        }

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
#endif
