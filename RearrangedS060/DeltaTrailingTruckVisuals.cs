#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using DV.Wheels;
using LocoSim.Implementations.Wheels;
using UnityEngine;

namespace RearrangedS060
{
    // Presentation only. Native bogies, axles, powered-wheel registrations,
    // simulation ports and wheel rotation components remain authoritative.
    internal sealed class DeltaTrailingTruckVisuals : MonoBehaviour
    {
        internal const string VisualRootName = "[Rearranged Delta visual]";
        private const float SourceRadius = .459f;
        private const string SplitBodyPath = "LocoS282A_Body/Static_LOD0/s282a_split_body(Clone)/";
        private static readonly string[] HiddenBodyParts =
        {
            "s282a_subframe_left", "s282a_subframe_right",
            "s282a_trailing_suspension_l", "s282a_trailing_suspension_r"
        };

        private Component owner;
        private GameObject body;
        private TrainCar car;
        private bool originalBodyActive;
        private readonly Dictionary<GameObject, bool> bodyRoots = new Dictionary<GameObject, bool>();
        private readonly Dictionary<Renderer, bool> hiddenRenderers = new Dictionary<Renderer, bool>();
        private readonly Dictionary<GameObject, bool> hiddenParts = new Dictionary<GameObject, bool>();
        private readonly List<GameObject> visualRoots = new List<GameObject>();
        private bool warningReported;
        private bool lastExploded;

        internal void Bind(Component changeOwner, GameObject changeBody)
        {
            Clear();
            if (changeOwner == null || changeBody == null) return;
            owner = changeOwner;
            body = changeBody;
            if (body.GetComponent<DeltaPresentationMarker>() == null) body.AddComponent<DeltaPresentationMarker>();
            car = GetComponent<TrainCar>();
            originalBodyActive = body.activeSelf;
            foreach (string path in new[] { "Axle_F", "Axle_R" })
            {
                var root = body.transform.Find(path);
                if (root != null) bodyRoots[root.gameObject] = root.gameObject.activeSelf;
            }
            enabled = true;
            Refresh();
        }

        // Must run before the arrangement captures or restores geometry: the
        // stock snapshot must contain the native visibility, not Delta's hiding.
        internal void Suspend()
        {
            foreach (var entry in hiddenRenderers)
                TryCleanup(() => { if (entry.Key != null) entry.Key.enabled = entry.Value; });
            hiddenRenderers.Clear();
            foreach (var entry in hiddenParts)
                TryCleanup(() => { if (entry.Key != null) entry.Key.SetActive(entry.Value); });
            hiddenParts.Clear();
            foreach (var root in visualRoots)
            {
                TryCleanup(() => { if (root != null) root.SetActive(false); });
                // SetupRearBogie scans direct children before native destruction
                // is processed. Detach inactive visuals immediately so they can
                // never be mistaken for a physical axle during that same frame.
                TryCleanup(() => { if (root != null) root.transform.SetParent(null, true); });
            }
            foreach (var entry in bodyRoots)
                TryCleanup(() => { if (entry.Key != null) entry.Key.SetActive(entry.Value); });
            TryCleanup(() => { if (body != null) body.SetActive(false); });
        }

        internal void Refresh()
        {
            Suspend();
            DestroyVisuals();
            if (owner == null || body == null || car == null) { Clear(); return; }
            lastExploded = car.isExploded;
            if (lastExploded) return;
            try
            {
                var rear = car.RearBogie;
                var rearRoot = rear != null ? rear.transform.Find("bogie_car") : null;
                if (rearRoot == null) return;
                var trailing = rear.Axles.Select(a => a.transform)
                    .Where(t => t != null && t.gameObject.activeInHierarchy && !IsPowered(t)).Distinct().ToArray();
                // Delta is a single-axle trailing truck. Keep its selection but
                // suppress the presentation on arrangements with zero/two axles.
                if (trailing.Length != 1) return;
                var axle = trailing[0];
                var rotations = car.GetComponentsInChildren<WheelRotationViaCode>(true)
                    .Where(r => r.transformsToRotate != null && r.transformsToRotate.Contains(axle)).ToArray();
                if (rotations.Length != 1 || !PositiveFinite(rotations[0].wheelRadius))
                    throw new InvalidOperationException("A unique native rolling-wheel radius was not found.");
                float radius = rotations[0].wheelRadius;
                var sourceBogie = body.transform.Find("Axle_R/bogie_car");
                if (sourceBogie == null) throw new InvalidOperationException("Delta rear bogie visual source was not found.");
                var sourceAxles = Enumerable.Range(0, sourceBogie.childCount).Select(sourceBogie.GetChild)
                    .Where(t => !IsPowered(t) && t.Find("axleR_model") != null).ToArray();
                var sourceFrame = sourceBogie.Find("Delta trailing truck");
                if (sourceAxles.Length != 1 || sourceFrame == null)
                    throw new InvalidOperationException("Delta wheel/frame visual source was not found uniquely.");
                var sourceAxle = sourceAxles[0];
                var sourceWheel = sourceAxle.Find("axleR_model");
                var worldScale = new Vector3(1f, radius / SourceRadius, radius / SourceRadius);

                // Capture only native presentation before adding our children.
                foreach (var renderer in axle.GetComponentsInChildren<Renderer>(true))
                {
                    if (IsVisualTransform(renderer.transform)) continue;
                    hiddenRenderers[renderer] = renderer.enabled;
                    renderer.enabled = false;
                }
                foreach (string name in HiddenBodyParts)
                {
                    var part = car.transform.Find(SplitBodyPath + name);
                    if (part == null) continue;
                    hiddenParts[part.gameObject] = part.gameObject.activeSelf;
                    part.gameObject.SetActive(false);
                }

                var wheelRoot = CreateRoot(axle, " wheel");
                wheelRoot.localPosition = Vector3.zero;
                wheelRoot.localRotation = Quaternion.identity;
                wheelRoot.localScale = CompensateScale(worldScale, axle.lossyScale);
                CloneRenderHierarchy(sourceWheel, wheelRoot);

                // The frame follows the bogie, never the spinning axle. Preserve
                // the authored frame-to-wheel offset, scaling Y/Z only so the
                // gauge/width stays unchanged when a smaller trailer is selected.
                var frameRoot = CreateRoot(rear.transform, " frame");
                frameRoot.rotation = rearRoot.rotation;
                frameRoot.localScale = CompensateScale(worldScale, rear.transform.lossyScale);
                frameRoot.position = axle.position - rearRoot.rotation * Vector3.Scale(sourceAxle.localPosition, worldScale);
                CloneRenderHierarchy(sourceFrame, frameRoot);

                body.SetActive(true);
                foreach (var entry in bodyRoots) if (entry.Key != null) entry.Key.SetActive(false);
            }
            catch (Exception ex)
            {
                // A visual incompatibility must never interrupt radio removal,
                // native deletion, or a successful arrangement conversion.
                Suspend();
                DestroyVisuals();
                if (!warningReported)
                {
                    Main.Logger.Warning("Delta trailing truck presentation was suppressed: " + ex.Message);
                    warningReported = true;
                }
            }
        }

        internal void Clear(Component changeOwner = null)
        {
            if (!ReferenceEquals(changeOwner, null) && !ReferenceEquals(owner, changeOwner)) return;
            Suspend();
            DestroyVisuals();
            TryCleanup(() => { if (body != null) body.SetActive(originalBodyActive); });
            bodyRoots.Clear();
            owner = null;
            body = null;
            car = null;
            warningReported = false;
            lastExploded = false;
            enabled = false;
        }

        private Transform CreateRoot(Transform parent, string suffix)
        {
            var root = new GameObject(VisualRootName + suffix);
            root.SetActive(false);
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);
            root.AddComponent<DeltaPresentationMarker>();
            visualRoots.Add(root);
            root.SetActive(true);
            return root.transform;
        }

        private static void CloneRenderHierarchy(Transform source, Transform parent)
        {
            var renderers = new Dictionary<Renderer, Renderer>();
            var groups = new List<KeyValuePair<LODGroup, LODGroup>>();
            CloneNode(source, parent, renderers, groups);
            // Reconstruct presentation-only LOD bindings. Cloning every LOD mesh
            // without its group would render overlapping resolutions together.
            foreach (var entry in groups)
            {
                var lods = entry.Key.GetLODs();
                var replacements = new LOD[lods.Length];
                for (int i = 0; i < lods.Length; i++)
                {
                    replacements[i] = new LOD(lods[i].screenRelativeTransitionHeight,
                        lods[i].renderers.Where(r => r != null && renderers.ContainsKey(r)).Select(r => renderers[r]).ToArray())
                    { fadeTransitionWidth = lods[i].fadeTransitionWidth };
                }
                entry.Value.SetLODs(replacements);
            }
        }

        private static void CloneNode(Transform source, Transform parent,
            Dictionary<Renderer, Renderer> renderers, List<KeyValuePair<LODGroup, LODGroup>> groups)
        {
            var copy = new GameObject(source.name);
            copy.SetActive(false);
            copy.layer = source.gameObject.layer;
            copy.transform.SetParent(parent, false);
            copy.transform.localPosition = source.localPosition;
            copy.transform.localRotation = source.localRotation;
            copy.transform.localScale = source.localScale;
            var sourceFilter = source.GetComponent<MeshFilter>();
            var sourceRenderer = source.GetComponent<MeshRenderer>();
            if (sourceFilter != null && sourceRenderer != null)
            {
                copy.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                var renderer = copy.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = sourceRenderer.sharedMaterials;
                renderer.enabled = sourceRenderer.enabled;
                renderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                renderer.receiveShadows = sourceRenderer.receiveShadows;
                renderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
                renderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
                renderer.probeAnchor = sourceRenderer.probeAnchor;
                renderer.sortingLayerID = sourceRenderer.sortingLayerID;
                renderer.sortingOrder = sourceRenderer.sortingOrder;
                renderers[sourceRenderer] = renderer;
            }
            var sourceGroup = source.GetComponent<LODGroup>();
            if (sourceGroup != null)
            {
                var group = copy.AddComponent<LODGroup>();
                group.enabled = sourceGroup.enabled;
                group.fadeMode = sourceGroup.fadeMode;
                group.animateCrossFading = sourceGroup.animateCrossFading;
                group.localReferencePoint = sourceGroup.localReferencePoint;
                group.size = sourceGroup.size;
                groups.Add(new KeyValuePair<LODGroup, LODGroup>(sourceGroup, group));
            }
            for (int i = 0; i < source.childCount; i++) CloneNode(source.GetChild(i), copy.transform, renderers, groups);
            copy.SetActive(source.gameObject.activeSelf);
        }

        private static bool IsPowered(Transform axle)
        {
            return axle.GetComponentsInChildren<Component>(true).Any(c => c != null &&
                (c is PoweredWheel || c.GetType().FullName == "CarChanger.Common.Components.PoweredAxle"));
        }

        private static Vector3 CompensateScale(Vector3 desiredWorld, Vector3 parentWorld)
        {
            if (!PositiveFinite(Mathf.Abs(parentWorld.x)) || !PositiveFinite(Mathf.Abs(parentWorld.y)) || !PositiveFinite(Mathf.Abs(parentWorld.z)))
                throw new InvalidOperationException("The trailing visual's parent has an invalid world scale.");
            return new Vector3(desiredWorld.x / Mathf.Abs(parentWorld.x), desiredWorld.y / Mathf.Abs(parentWorld.y), desiredWorld.z / Mathf.Abs(parentWorld.z));
        }

        private static bool PositiveFinite(float value) => value > .00001f && !float.IsInfinity(value) && !float.IsNaN(value);

        internal static bool IsVisualTransform(Transform transform)
        {
            for (var current = transform; current != null; current = current.parent)
                if (current.name.StartsWith(VisualRootName, StringComparison.Ordinal)) return true;
            return false;
        }

        private void DestroyVisuals()
        {
            foreach (var root in visualRoots)
            {
                if (root == null) continue;
                TryCleanup(() => root.SetActive(false));
                TryCleanup(() => root.transform.SetParent(null, true));
                TryCleanup(() => UnityEngine.Object.Destroy(root));
            }
            visualRoots.Clear();
        }

        private void TryCleanup(Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                // Cleanup runs inside native car deletion as well as radio
                // removal. A presentation failure must not abort either path.
                if (warningReported) return;
                warningReported = true;
                try { Main.Logger.Warning("Delta visual cleanup could not restore one presentation item: " + ex.Message); }
                catch { }
            }
        }

        private void LateUpdate()
        {
            if (owner == null || body == null) Clear();
            else if (car != null && car.isExploded != lastExploded)
            {
                lastExploded = car.isExploded;
                if (lastExploded) Suspend();
                else Refresh();
            }
        }

        private void OnDestroy() { Clear(); }
    }
}
#endif
