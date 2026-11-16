#if B99_S060_FUNCTIONAL
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using DV.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Native S282 big-end recipe, scoped to this S060. Two cups per driven axle,
    // no more (James, 2026-10-28): each side has one cup per axle on the circuits
    // S060OilingTopology.SideCircuits names. Conventional layouts keep the native
    // guide/crosshead/big-end cups where they are and add big ends on the axles
    // furthest from them; articulated layouts seat every cup upright on a crank boss.
    // A native cup without a live circuit is hidden. No decorative dummy cups.
    [DefaultExecutionOrder(2000)]
    internal sealed class S060OilCups : MonoBehaviour
    {
        private sealed class Mount
        {
            internal Transform Frame, Wheel, Fixed, Marker, Stem;
            internal PositionSyncConsumer Consumer;
            internal PositionSyncProvider Original, Provider;
            internal Quaternion OriginalRotation;
            internal Vector3 FixedPoint, CupFoot;
            internal Quaternion CupOrientation;
            internal float X, Lift, Boss;
        }
        private TrainCar car;
        private GameObject external;
        private Coroutine pending;
        private Mesh stemMesh;
        private MeshRenderer stemMaterialSource;
        private Material stemMaterial;
        private readonly List<Mount> mounts = new List<Mount>();
        private S060OilCupInteractables interactables;
        private readonly List<GameObject> supports = new List<GameObject>();
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private readonly List<Collider> hiddenColliders = new List<Collider>();
        internal int CupCount { get; private set; }
        internal bool ServicingReady { get; private set; }
        internal static void Attach(TrainCar car)
        {
            if (!S060Parts.IsS060(car)) return;
            var owner = car.GetComponent<S060OilCups>() ?? car.gameObject.AddComponent<S060OilCups>();
            if (owner.car != car)
            {
                if (owner.car != null) owner.car.ExternalInteractableLoaded -= owner.ExternalLoaded;
                owner.car = car; car.ExternalInteractableLoaded += owner.ExternalLoaded;
            }
            owner.Refresh();
        }
        internal static void Refresh(TrainCar car)
        { if (car != null) Attach(car); }
        internal static void Release(TrainCar car)
        {
            var owner = car != null ? car.GetComponent<S060OilCups>() : null;
            if (owner == null) return;
            owner.Cancel(); owner.Clear();
            if (owner.car != null) owner.car.ExternalInteractableLoaded -= owner.ExternalLoaded;
            owner.car = null; owner.external = null;
        }
        private void OnDestroy()
        { Cancel(); Clear(); if (car != null) car.ExternalInteractableLoaded -= ExternalLoaded; }
        private void OnEnable() { if (car != null) Refresh(); }
        private void OnDisable() => Cancel();
        private void ExternalLoaded(GameObject value) { external = value; Refresh(); }
        private void Cancel() { if (pending != null) StopCoroutine(pending); pending = null; }
        private void Refresh()
        {
            Cancel(); Clear(); if (car == null || !isActiveAndEnabled) return;
            external = car.loadedExternalInteractables;
            pending = StartCoroutine(BuildLater());
        }
        private IEnumerator BuildLater()
        {
            // Native interactable controllers finish their delayed startup first,
            // as in S282; otherwise Init/port listeners are installed twice.
            yield return null; yield return null; yield return null;
            while (car != null && (car.loadedExternalInteractables == null ||
                car.transform.Find("LocoS060_Body/DrivingMechanism_R/s060_mech_wheels_connect") == null)) yield return null;
            if (car == null) { pending = null; yield break; }
            external = car.loadedExternalInteractables;
            var feeders = external.GetComponentInChildren<InteractablePortFeedersController>(true);
            while (feeders != null && feeders.IsCurrentlySettingInitialValues) yield return null;
            try { Build(); }
            catch (Exception ex) { Clear(); Main.Logger.LogException("S060 oil cup servicing unavailable; arrangement oil circuits remain selected", ex); }
            // Lids/controls have now spawned; bind only our copies once.
            yield return null; yield return null;
            interactables?.BindLids();
            pending = null;
        }
        private sealed class Site
        {
            internal Transform Frame, Wheel;
            internal bool Left, Native, FrontUnit, Main;
            internal float X, Lift, Boss;
        }
        private List<Site> Sites()
        {
            var result = new List<Site>();
            var body = car.transform.Find("LocoS060_Body");
            var articulated = car.GetComponentsInChildren<Transform>(false).Where(t => t.name.StartsWith("[S060 Meyer ", StringComparison.Ordinal) || t.name.StartsWith("[S060 Dockside ", StringComparison.Ordinal)).ToArray();
            if (articulated.Length != 0)
            {
                if (articulated.Length != 2) throw new InvalidOperationException("Two articulated engine frames required for oil cups.");
                var frontFrame = articulated.OrderByDescending(t => car.transform.InverseTransformPoint(t.position).z).First();
                foreach (var frame in articulated)
                    foreach (bool left in new[] { true, false })
                    {
                        string suffix = left ? "L" : "R";
                        var axles = frame.Cast<Transform>().Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).OrderBy(t => t.localPosition.z).ToArray();
                        foreach (var axle in axles)
                        {
                            var wheel = axle.Cast<Transform>().Single(t => t.name.Contains("driver " + suffix));
                            bool main = axle == axles[0];
                            float rodX = (left ? -1 : 1) * (main ? S060OilCupGeometry.MainRodPlaneX : S060OilCupGeometry.CouplingRodPlaneX);
                            bool meyer = frame.name.StartsWith("[S060 Meyer", StringComparison.Ordinal);
                            result.Add(new Site { Frame = frame, Wheel = wheel, Left = PhysicalLeft(car.transform, frame, wheel, rodX), FrontUnit = frame == frontFrame, Main = main, Native = frame == frontFrame && main,
                                X = rodX, Lift = meyer ? (main ? .17f : .15f) : (main ? .12f : .11f), Boss = meyer ? (main ? .135675f : .114063f) : .067f });
                        }
                    }
                return result;
            }
            foreach (bool left in new[] { true, false })
            {
                var frame = body.Find(left ? "DrivingMechanism_L" : "DrivingMechanism_R");
                var wheels = frame.Cast<Transform>().Where(w => w.name.StartsWith("s060_Wheels_0", StringComparison.Ordinal) && !w.name.Contains("LOD") && w.gameObject.activeInHierarchy).ToArray();
                foreach (var wheel in wheels)
                    result.Add(new Site { Frame = frame, Wheel = wheel, Left = left, Native = wheel.name == "s060_Wheels_01",
                        X = .938477f, Lift = .14f * Mathf.Abs(frame.localScale.y), Boss = .114062f * Mathf.Abs(frame.localScale.y) });
            }
            return result;
        }
        // Local L/R exchanges sides on the inward-facing Meyer front unit.
        // Keep the original per-car oil circuits on their physical side while
        // the seat/rod coordinates remain local to their animated engine.
        internal static bool PhysicalLeft(Transform car, Transform frame, Transform wheel, float rodX) =>
            car.InverseTransformPoint(S060OilCupGeometry.Pin(frame, wheel, rodX)).x < 0;
        private void Build()
        {
            stemMaterialSource = car.transform.Find("LocoS060_Body").GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.name == "s060_body" && r.sharedMaterial != null).OrderByDescending(r => r.enabled && r.gameObject.activeInHierarchy ? 1 : 0)
                .ThenByDescending(r => r.GetComponent<MeshFilter>()?.sharedMesh?.vertexCount ?? 0).FirstOrDefault();
            if (stemMaterialSource == null) throw new InvalidOperationException("Live S060 body material missing for oil cup mounting necks.");
            stemMaterial = stemMaterialSource.sharedMaterial;
            interactables = new S060OilCupInteractables(car, external);
            var readers = interactables.NativeReaders();
            if (S060Conversion.Current(car) is S060Shay shay) { BuildShay(shay, readers); return; }
            var arrangement = (S060Conversion.Current(car)?.Selection ?? S060Selection.Stock).Arrangement;
            var sites = Sites();
            bool articulated = sites.Any(s => s.FrontUnit);
            foreach (bool left in new[] { true, false })
            {
                var circuits = S060OilingTopology.SideCircuits(arrangement, left);
                var side = sites.Where(s => s.Left == left).ToList();
                if (circuits.Length != side.Count) throw new InvalidOperationException("Oil cup circuits do not match the live driven axles.");
                int nativeIndex = S060OilingTopology.NativeBigEndIndex(left);
                foreach (int index in Enumerable.Range(left ? 0 : 3, 3).Where(i => !circuits.Contains(i)))
                    Hide(S060OilCupInteractables.Consumer(readers[index]));
                List<Site> axles;
                if (articulated)
                {
                    // Front engine before rear, main crank before coupled axle: the native big end first.
                    axles = side.OrderByDescending(s => s.FrontUnit).ThenByDescending(s => s.Main).ToList();
                    var upright = Quaternion.Inverse(car.transform.rotation) * S060OilCupInteractables.Consumer(readers[nativeIndex]).transform.rotation;
                    for (int i = 0; i < circuits.Length && circuits[i] < S060OilingTopology.NativePoints; i++)
                        FitNative(S060OilCupInteractables.Consumer(readers[circuits[i]]), axles[i], upright);
                }
                else
                {
                    int guideIndex = left ? 0 : 3;
                    if (circuits.Contains(guideIndex)) FitConventionalGuide(S060OilCupInteractables.Consumer(readers[guideIndex]), body: car.transform.Find("LocoS060_Body"));
                    // Native cups stay native; added big ends go furthest from the native crank first.
                    float nativeZ = side.Single(s => s.Native).Wheel.localPosition.z;
                    var free = side.Where(s => !s.Native).OrderByDescending(s => Mathf.Abs(s.Wheel.localPosition.z - nativeZ)).ToList();
                    axles = Enumerable.Repeat<Site>(null, circuits.Count(i => i < S060OilingTopology.NativePoints)).Concat(free).ToList();
                }
                for (int i = 0; i < circuits.Length; i++)
                {
                    if (circuits[i] < S060OilingTopology.NativePoints) continue;
                    var consumer = interactables.Clone(readers[nativeIndex], circuits[i], nativeIndex);
                    NewMount(axles[i], consumer, consumer.syncTag);
                    interactables.Register(consumer);
                }
                CupCount += circuits.Length;
            }
            if (CupCount != 2 * car.SimController.poweredWheels.poweredWheels.Length) throw new InvalidOperationException("Every live driven axle needs exactly two oil cups.");
            LateUpdate(); ServicingReady = true;
            Main.Logger.Log("S060 oiling: " + CupCount + " cups, two per driven axle; native quantities retained.");
        }
        private void BuildShay(S060Shay shay, OilingPointPortFeederReader[] readers)
        {
            // R2 (James, 2026-10-28): six upright cups on the six native circuits, extras dormant.
            // Seats are plain unrotated children of yaw-only frames, so no cup can lie sideways.
            foreach(var seat in S060ShaySeats.Build(shay))
            {
                var cup=S060OilCupInteractables.Consumer(readers[seat.Key]);
                var mount=NewMount(new Site{Frame=seat.Value},cup,cup.syncTag);mount.Fixed=seat.Value;mount.FixedPoint=Vector3.zero;
                mount.CupOrientation=Quaternion.identity;
                mount.Original=car.GetComponentsInChildren<PositionSyncProvider>(true).Single(p=>p.syncTag==cup.syncTag&&!p.name.StartsWith("[S060 oil",StringComparison.Ordinal));
                mount.OriginalRotation=cup.transform.localRotation;
            }
            CupCount=S060OilingTopology.NativePoints;LateUpdate();ServicingReady=true;
            Main.Logger.Log("S060 Shay oiling: six upright cups (three line-shaft bearings, two off-side truck frames, one crank bearing) on the native circuits.");
        }

        // A native cup moved onto a crank boss, standing as upright as the native big end.
        private void FitNative(PositionSyncConsumer cup, Site site, Quaternion orientation)
        {
            if (cup == null) throw new InvalidOperationException("Native oil cup sync consumer missing.");
            var provider = car.GetComponentsInChildren<PositionSyncProvider>(true).Single(p => p.syncTag == cup.syncTag && !p.name.StartsWith("[S060 oil", StringComparison.Ordinal));
            var mount = NewMount(site, cup, cup.syncTag); mount.Original = provider; mount.OriginalRotation = cup.transform.localRotation;
            mount.CupOrientation = orientation;
        }
        // The native upper slidebar is part of the rigid body, not the resized
        // mechanism. Keep its cup at the original body station, seated on the
        // live native rail, rather than scaling/translating its old provider.
        private void FitConventionalGuide(PositionSyncConsumer cup, Transform body)
        {
            var provider = car.GetComponentsInChildren<PositionSyncProvider>(true).Single(p => p.syncTag == cup.syncTag && !p.name.StartsWith("[S060 oil", StringComparison.Ordinal));
            var reference = provider.transform.localPosition;
            reference.x *= Mathf.Sign(provider.transform.parent.localScale.x);
            var seat = S060OilCupGeometry.GuideSeat(body, reference);
            var mount = NewMount(new Site { Frame = body }, cup, cup.syncTag);
            mount.Original = provider; mount.OriginalRotation = cup.transform.localRotation;
            mount.Fixed = body; mount.FixedPoint = seat;
        }
        // A native cup whose circuit is dormant in this layout: not drawn, not reachable.
        private void Hide(PositionSyncConsumer cup)
        {
            if (cup == null) throw new InvalidOperationException("Native oil cup sync consumer missing.");
            foreach (var r in cup.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled)) { r.enabled = false; hiddenRenderers.Add(r); }
            foreach (var c in cup.GetComponentsInChildren<Collider>(true).Where(c => c.enabled)) { c.enabled = false; hiddenColliders.Add(c); }
        }
        private Mount NewMount(Site site, PositionSyncConsumer cup, string tag)
        {
            var marker = new GameObject("[S060 oil marker " + tag + "]").transform; marker.SetParent(site.Frame, false); supports.Add(marker.gameObject);
            var provider = marker.gameObject.AddComponent<PositionSyncProvider>(); provider.syncTag = tag;
            var mount = new Mount { Frame = site.Frame, Wheel = site.Wheel, Marker = marker, Provider = provider, Consumer = cup, X = site.X, Lift = site.Lift, Boss = site.Boss, CupFoot = CupFoot(cup), CupOrientation = Quaternion.Inverse(car.transform.rotation) * cup.transform.rotation };
            mounts.Add(mount); cup.SetProviderTransform(provider);
            // The mounting neck seats on the rod boss and the measured native
            // cup thread; it does not use the mesh origin as its attachment.
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder); stem.name = "[S060 oil mounting neck]";
            stem.transform.SetParent(site.Frame, false);
            var collider = stem.GetComponent<Collider>(); collider.enabled = false; DestroyOwned(collider);
            if (stemMesh == null)
            {
                stemMesh = Object.Instantiate(stem.GetComponent<MeshFilter>().sharedMesh); stemMesh.name = "[S060 oil neck iron patch]";
                var uv = stemMesh.uv;
                for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(.6868f, .8948f) + (uv[i] - Vector2.one * .5f) * .012f;
                stemMesh.uv = uv; stemMesh.RecalculateTangents();
            }
            stem.GetComponent<MeshFilter>().sharedMesh = stemMesh;
            stem.GetComponent<MeshRenderer>().sharedMaterial = stemMaterial;
            mount.Stem = stem.transform; supports.Add(stem); return mount;
        }
        private void LateUpdate()
        {
            if (stemMaterialSource != null && stemMaterialSource.sharedMaterial != stemMaterial)
            {
                stemMaterial = stemMaterialSource.sharedMaterial;
                foreach (var m in mounts) if (m.Stem != null) m.Stem.GetComponent<MeshRenderer>().sharedMaterial = stemMaterial;
            }
            foreach (var mount in mounts)
            {
                if (mount.Frame == null || mount.Consumer == null) continue;
                Vector3 basePoint, contact;
                var cupRotation = mount.Frame.rotation * mount.CupOrientation;
                if (mount.Fixed != null)
                { contact = mount.Fixed.TransformPoint(mount.FixedPoint); basePoint = S060OilCupGeometry.SeatedOrigin(mount.Frame, contact, mount.CupFoot, cupRotation); }
                else
                {
                    if (mount.Wheel == null) continue;
                    var pin = S060OilCupGeometry.Pin(mount.Frame, mount.Wheel, mount.X); contact = pin + mount.Frame.up * mount.Boss; basePoint = pin + mount.Frame.up * mount.Lift;
                }
                mount.Marker.position = basePoint; mount.Consumer.Sync(); mount.Consumer.transform.rotation = cupRotation;
                var threadTop = S060OilCupGeometry.ThreadTop(mount.Frame, basePoint, mount.CupFoot, cupRotation);
                var delta = threadTop - contact; mount.Stem.position = (contact + threadTop) / 2;
                mount.Stem.rotation = delta.sqrMagnitude > .000001f ? Quaternion.FromToRotation(Vector3.up, delta.normalized) : mount.Frame.rotation;
                mount.Stem.localScale = new Vector3(.035f, Mathf.Max(.005f, delta.magnitude / 2) / Mathf.Max(.001f, Mathf.Abs(mount.Frame.lossyScale.y)), .035f / Mathf.Max(.001f, Mathf.Abs(mount.Frame.lossyScale.z)));
                mount.Stem.gameObject.SetActive(delta.magnitude > .003f);
            }
        }
        private static Vector3 CupFoot(PositionSyncConsumer cup)
        {
            var filter = cup.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f => f.name == "OilCup_LOD0" && f.sharedMesh != null);
            if (filter == null) throw new InvalidOperationException("Native oil cup body mesh missing.");
            var b = filter.sharedMesh.bounds;
            var nativeFoot = S060OilCupGeometry.NativeThreadFoot(filter.sharedMesh.name, b);
            return cup.transform.InverseTransformPoint(filter.transform.TransformPoint(nativeFoot));
        }
        private void Clear()
        {
            ServicingReady = false;
            foreach (var mount in mounts)
                if (mount.Consumer != null && mount.Original != null)
                { mount.Consumer.SetProviderTransform(mount.Original); mount.Consumer.transform.localRotation = mount.OriginalRotation; mount.Consumer.Sync(); }
            mounts.Clear();
            foreach (var r in hiddenRenderers) if (r != null) r.enabled = true; hiddenRenderers.Clear();
            foreach (var c in hiddenColliders) if (c != null) c.enabled = true; hiddenColliders.Clear();
            interactables?.Dispose(); interactables = null;
            foreach (var support in supports.Where(s => s != null)) { support.SetActive(false); DestroyOwned(support); }
            supports.Clear(); CupCount = 0;
            if (stemMesh != null) DestroyOwned(stemMesh); stemMesh = null; stemMaterialSource = null; stemMaterial = null;
        }
        private static void DestroyOwned(Object value) { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
#endif
