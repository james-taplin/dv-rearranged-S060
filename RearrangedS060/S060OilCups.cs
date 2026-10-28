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
    // Native S282 big-end recipe, scoped to this S060. Native guide/crosshead
    // cups remain functional; each driven axle additionally has one big end per
    // side, including the attached rear power unit. No decorative dummy cups.
    [DefaultExecutionOrder(2000)]
    internal sealed class S060OilCups : MonoBehaviour
    {
        private sealed class Mount
        {
            internal Transform Frame, Wheel, Crosshead, Fixed, Marker, Stem;
            internal PositionSyncConsumer Consumer;
            internal PositionSyncProvider Original, Provider;
            internal Quaternion OriginalRotation;
            internal Vector3 FixedPoint, CupFoot;
            internal Quaternion CupOrientation;
            internal float X, Lift, Boss, CrossheadOffsetX;
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
        internal int BigEndCount { get; private set; }
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
            internal Transform Frame, Wheel, Crosshead, Guide;
            internal bool Left, Native;
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
                        var rodRoot = frame.Find("[Meyer rods " + suffix + "]") ?? frame.Find("[Dockside rods " + suffix + "]");
                        var axles = frame.Cast<Transform>().Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).OrderBy(t => t.localPosition.z).ToArray();
                        foreach (var axle in axles)
                        {
                            var wheel = axle.Cast<Transform>().Single(t => t.name.Contains("driver " + suffix));
                            bool native = frame == frontFrame && axle == axles[0];
                            result.Add(new Site { Frame = frame, Wheel = wheel, Left = left, Native = native,
                                Crosshead = rodRoot.Find("Crosshead"), Guide = rodRoot.Find("Upper crosshead guide") ?? rodRoot.Find("Crosshead guide 1"),
                                X = (left ? -1 : 1) * (axle == axles[0] ? S060OilCupGeometry.MainRodPlaneX : S060OilCupGeometry.CouplingRodPlaneX), Lift = axle == axles[0] ? .12f : .11f, Boss = frame.name.StartsWith("[S060 Meyer", StringComparison.Ordinal) ? .085f : .067f });
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
                        X = .938477f, Lift = .14f * Mathf.Abs(frame.localScale.y), Boss = .105f * Mathf.Abs(frame.localScale.y) });
            }
            return result;
        }
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
            var sites = Sites();
            foreach (bool left in new[] { true, false })
            {
                var nativeSite = sites.Single(s => s.Left == left && s.Native);
                int nativeIndex = S060OilingTopology.NativeBigEndIndex(left);
                if (nativeSite.Crosshead != null)
                {
                    // Retain the guide/crosshead/big-end circuits while their
                    // visible engine unit replaces the hidden native mechanism.
                    FitNative(S060OilCupInteractables.Consumer(readers[left ? 0 : 3]), nativeSite, 0);
                    FitNative(S060OilCupInteractables.Consumer(readers[left ? 1 : 4]), nativeSite, 1);
                    FitNative(S060OilCupInteractables.Consumer(readers[nativeIndex]), nativeSite, 2);
                }
                int slot = 0;
                foreach (var site in sites.Where(s => s.Left == left && !s.Native).OrderByDescending(s => car.transform.InverseTransformPoint(s.Wheel.position).z))
                {
                    int index = S060OilingTopology.ExtraIndex(left, slot++);
                    var consumer = interactables.Clone(readers[nativeIndex], index, nativeIndex);
                    NewMount(site, consumer, consumer.syncTag);
                    interactables.Register(consumer);
                }
            }
            BigEndCount = sites.Count;
            if (BigEndCount != 2 * car.SimController.poweredWheels.poweredWheels.Length) throw new InvalidOperationException("Every live driven axle needs two big-end cups.");
            LateUpdate(); ServicingReady = true;
            Main.Logger.Log("S060 oiling: " + BigEndCount + " mounted big ends plus four native guide/crosshead cups; native quantities retained.");
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
            BigEndCount=S060OilingTopology.NativePoints;LateUpdate();ServicingReady=true;
            Main.Logger.Log("S060 Shay oiling: six upright cups (three line-shaft bearings, two off-side truck frames, one crank bearing) on the native circuits.");
        }

        private void FitNative(PositionSyncConsumer cup, Site site, int kind)
        {
            if (cup == null) throw new InvalidOperationException("Native oil cup sync consumer missing.");
            var provider = car.GetComponentsInChildren<PositionSyncProvider>(true).Single(p => p.syncTag == cup.syncTag && !p.name.StartsWith("[S060 oil", StringComparison.Ordinal));
            var mount = NewMount(site, cup, cup.syncTag); mount.Original = provider; mount.OriginalRotation = cup.transform.localRotation;
            if (kind == 0)
            {
                if (site.Guide == null) throw new InvalidOperationException("Articulated guide oil cup has no support.");
                mount.Wheel = null; mount.Fixed = site.Guide;
                mount.FixedPoint = new Vector3(0, .5f, 0); // actual guide mesh top, local cube coordinates
            }
            else if (kind == 1)
            { mount.Wheel = null; mount.Crosshead = site.Crosshead; mount.CrossheadOffsetX = (site.Left ? -1 : 1) * .0877f; }
            // kind2 follows the driven crank, on its main-rod big-end plane.
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
            var collider = stem.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider);
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
                else if (mount.Crosshead != null)
                { contact = mount.Crosshead.TransformPoint(new Vector3(0, .5f, 0));
                    basePoint = contact - mount.Frame.up * (Vector3.Dot(cupRotation * mount.CupFoot, mount.Frame.up) + S060OilCupGeometry.ThreadEmbed) + mount.Frame.right * mount.CrossheadOffsetX;
                    var actualThread = S060OilCupGeometry.ThreadTop(mount.Frame, basePoint, mount.CupFoot, cupRotation);
                    var onBlock = mount.Crosshead.InverseTransformPoint(actualThread);
                    contact = mount.Crosshead.TransformPoint(new Vector3(onBlock.x, .5f, onBlock.z)); }
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
            interactables?.Dispose(); interactables = null;
            foreach (var support in supports.Where(s => s != null)) { support.SetActive(false); Object.Destroy(support); }
            supports.Clear(); BigEndCount = 0;
            if (stemMesh != null) Object.Destroy(stemMesh); stemMesh = null; stemMaterialSource = null; stemMaterial = null;
        }
    }
}
#endif
