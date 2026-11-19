#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using DV;
using DV.Wheels;
using LocoSim.Implementations.Wheels;
using UnityEngine;

namespace RearrangedS060
{
    internal sealed class S060Meyer : S060Articulated
    {
        protected override bool IsMeyer => true;
        internal override string Name => "0-4+4-0T Meyer compound";
    }
    internal sealed class S060Dockside : S060Articulated
    {
        protected override bool IsMeyer => false;
        internal override string Name => "0-4-4-0T Franklin Dockside";
    }
    internal abstract class S060Articulated : S060Conversion
    {
        protected abstract bool IsMeyer { get; }
        private readonly List<GameObject> added = new List<GameObject>();
        private readonly List<(GameObject part, bool active)> hidden = new List<(GameObject, bool)>();
        private readonly List<(Renderer renderer, bool enabled)> renderers = new List<(Renderer, bool)>();
        private readonly List<(Behaviour component, bool enabled)> readers = new List<(Behaviour, bool)>();
        private readonly List<S060ArticulatedPresentation> presentation = new List<S060ArticulatedPresentation>();
        private S060ArticulatedSimulation simulation;
        private S060StockCocks stockCocks;
        private S060MeyerSupport meyerSupport;
        private S060SteamPipes steamPipes;
        private S060NativeCylinders nativeCylinders;
        private S060MeyerClearance meyerClearance;
        private S060MeyerFrontMotion meyerFrontMotion;
        private readonly List<S060MeyerGearMounts> meyerGearMounts = new List<S060MeyerGearMounts>();
        private S060MeyerRunningGear meyerRunningGear;
        private S060MeyerFrontEquipment meyerFrontEquipment;
        private S060MeyerLights meyerLights;
        private S060NativeQuartering nativeQuartering;
        private S060MeyerBodywork bodywork;
        private S060IndependentDrive independentDrive;
        private S060MeyerCapacity capacity;

        internal static Transform[] Preflight(bool meyer) => ResolveFrames(meyer, out _);
        private static Transform[] ResolveFrames(bool meyer, out Transform donorRoot)
        {
            string id = meyer ? "RS060_Meyer_0440" : "RS060_Franklin_Dockside_0440";
            if (Globals.G?.Types == null || !Globals.G.Types.TryGetLivery(id, out var donor) || donor.prefab == null)
                throw new InvalidOperationException((meyer ? "Meyer" : "Dockside") + " assets not loaded. Check Custom Car Loader (CCL) and its dependencies, including DV Language Helper, in UMM; restart the game. Packs are bundled. See Player.log for CCL errors.");
            donorRoot = donor.prefab.transform;
            S060StereoShaders.Apply(donor.prefab);
            var frames = donor.prefab.GetComponentsInChildren<Transform>(true).Where(t => t.name == "bogie_car").OrderBy(t => donor.prefab.transform.InverseTransformPoint(t.position).z).ToArray();
            if (frames.Length != 2) throw new InvalidOperationException("Articulated donor must have exactly two bogie frames.");
            if (frames.Any(f => f.Find("[S060 fitted effects 0.7]") == null))
                throw new InvalidOperationException("Update the Meyer/Dockside asset pack to 0.7.0 for fitted cylinder effects before selecting it.");
            if (frames.Any(f => f.GetComponent<Animator>() == null || f.Find("[Powered bogie frame]") == null || f.Find("[S060 fitted gear 0.6]") == null ||
                (f.Find("[Round cylinders R]") == null && f.Find("[Native S060 cylinders]") == null)))
                throw new InvalidOperationException("Update the " + (meyer ? "Meyer" : "Dockside") + " asset pack to 0.6.0 before selecting it.");
            foreach (var frame in frames)
            {
                var axles = frame.Cast<Transform>().Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).ToArray();
                if (axles.Length != 2 || axles.Any(a => new[] { "L", "R" }.Any(side =>
                    a.Cast<Transform>().Count(w => w.name.Contains("driver " + side)) != 1)))
                    throw new InvalidOperationException("Articulated donor must have two axles with one left and right driver per frame.");
            }
            if (meyer)
            {
                if (frames[1].Find("[Native S060 cylinders]") == null)
                    throw new InvalidOperationException("Meyer requires the 0.6.0 pack with native front-cylinder fitting.");
                if (Mathf.Abs(donor.prefab.transform.InverseTransformPoint(frames[0].position).z - MeyerPlan.RearCentre) > .01f)
                    throw new InvalidOperationException("Meyer needs the attached rear steam-bogie asset pack 0.6.0.");
            }
            else if (frames[1].Find("[Dockside smokebox sleeve L]")?.GetComponent<MeshFilter>()?.sharedMesh == null ||
                frames[1].Find("[Dockside smokebox sleeve R]")?.GetComponent<MeshFilter>()?.sharedMesh == null)
                throw new InvalidOperationException("Dockside needs the 0.3.5 pack with prebuilt steam-pipe sleeves.");
            return frames;
        }

        protected override void Apply()
        {
            if (Scale.Factor != 1) throw new InvalidOperationException("Articulated layouts use their authored wheel size.");
            simulation = S060ArticulatedSimulation.Ensure(Parts.Car);
            var frames = ResolveFrames(IsMeyer, out var donorRoot);
            nativeQuartering = new S060NativeQuartering(Parts.MechanismLeft, Parts.MechanismRight);
            nativeQuartering.Retain();
            if (IsMeyer) { meyerSupport = S060MeyerSupport.For(Parts.Car); meyerSupport.Apply(IsRestoring); }
            stockCocks = new S060StockCocks(Parts.Car); stockCocks.Mute();
            // Keep native bogies, joints, track travellers and identities. Like
            // the proven 0-4-0, axle offsets may differ from the support pivots.
            foreach (var mechanism in new[] { Parts.MechanismLeft, Parts.MechanismRight }) Hide(mechanism.gameObject);
            for (int i = 1; i <= 3; i++) Hide(Parts.AxleFor(i).gameObject);
            foreach (var renderer in Parts.Body.GetComponentsInChildren<Renderer>(true).Where(r =>
                r.name.StartsWith("s060_cylinder_", StringComparison.Ordinal) || r.name.IndexOf("caliper", StringComparison.OrdinalIgnoreCase) >= 0 ||
                r.name.IndexOf("brake_shoes", StringComparison.OrdinalIgnoreCase) >= 0))
            { renderers.Add((renderer, renderer.enabled)); renderer.enabled = false; }
            foreach (var reader in Parts.Car.GetComponentsInChildren<PoweredWheelRotationViaAnimation>(true))
            { readers.Add((reader, reader.enabled)); reader.enabled = false; }
            var powered = new List<PoweredWheel>();
            var liveFrames = new List<Transform>();
            for (int i = 0; i < 2; i++)
            {
                var bogie = i == 0 ? Parts.RearBogie : Parts.FrontBogie;
                var parent = bogie.transform.Find("bogie_car") ?? throw new InvalidOperationException("Native bogie frame is missing.");
                var source = frames[i];
                var frame = UnityEngine.Object.Instantiate(source.gameObject, parent); added.Add(frame);
                frame.name = "[S060 " + (IsMeyer ? "Meyer" : "Dockside") + (i == 0 ? " rear]" : " front]");
                liveFrames.Add(frame.transform);
                if (IsMeyer) frame.transform.localPosition = i == 0 ? S060MeyerLayout.RearVisualOffset : Vector3.zero;
                else frame.transform.position = Parts.Car.transform.TransformPoint(donorRoot.InverseTransformPoint(source.position));
                frame.transform.localRotation = source.localRotation; frame.transform.localScale = source.localScale;
                if (IsMeyer && i == 1) frame.transform.localRotation = S060MeyerLayout.FrontRotation * source.localRotation;
                // Visual axles stay inside the authored Animator hierarchy. Four
                // plain native physics axles live directly below bogie_car,
                // where DV's axle getter expects them, and point at those wheels.
                var visualAxles = frame.transform.Cast<Transform>().Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal)).OrderBy(t => t.localPosition.z).ToArray();
                if (visualAxles.Length != 2) throw new InvalidOperationException("Articulated frame has the wrong axle count.");
                foreach (var axle in visualAxles)
                {
                    var wheel = axle.Cast<Transform>().SingleOrDefault(t => t.name.Contains("driver R"))
                        ?? throw new InvalidOperationException("Articulated right wheel is missing.");
                    var live = UnityEngine.Object.Instantiate(Parts.AxleFor(1).gameObject, parent); added.Add(live);
                    live.name = "[axle] S060 articulated " + powered.Count;
                    live.transform.position = axle.position; live.transform.localRotation = Quaternion.identity;
                    foreach (var r in live.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                    var pw = live.GetComponent<PoweredWheel>() ?? throw new InvalidOperationException("Native powered axle component is missing.");
                    pw.wheelTransform = wheel; powered.Add(pw); live.SetActive(true);
                    foreach (var inherited in axle.GetComponents<PoweredWheel>()) inherited.enabled = false;
                }
                frame.SetActive(true); bogie.axles = null;
                var mode = IsMeyer ? S060EngineMode.Meyer : S060EngineMode.Dockside;
                string engine = i == 0 ? S060EngineMapping.PhysicalRearId(mode) : S060EngineMapping.PhysicalFrontId(mode);
                var effects = new S060ArticulatedPresentation(Parts.Car, frame, engine, true,
                    reverseVisual: IsMeyer && i == 1);
                presentation.Add(effects); effects.Bind();
            }
            Physics.Apply(IsMeyer ? S060Parts.DriverRadius : .5f, powered.ToArray(), 4, 1);
            if (IsMeyer) meyerSupport.BindCollisionFrame(liveFrames[0]);
            if (IsMeyer) meyerFrontMotion = S060MeyerFrontMotion.Bind(liveFrames[1]);
            if (IsMeyer) foreach (var frame in liveFrames) meyerGearMounts.Add(S060MeyerGearMounts.Bind(frame, Parts.Body));
            else foreach (var frame in liveFrames) meyerGearMounts.Add(S060MeyerGearMounts.BindWheelCranks(frame));
            if (IsMeyer) meyerRunningGear=S060MeyerRunningGear.Bind(Parts.Car.transform,Parts.Body,liveFrames[1],liveFrames[0]);
            if (IsMeyer) nativeCylinders = S060NativeCylinders.Bind(Parts, liveFrames[1]);
            if (IsMeyer)
            {
                // R2: tanks, hopper and rear end are rigid bodywork on the S060 body. A fixed datum at
                // the donor rear frame's authored position carries the lamps and buffers with the body.
                bodywork = S060MeyerBodywork.Apply(Parts.Car, Parts.Body, liveFrames[0]);
                var datum = new GameObject("[Meyer R2 rear datum]"); added.Add(datum);
                datum.transform.SetParent(Parts.Car.transform, false);
                datum.transform.localPosition = donorRoot.InverseTransformPoint(frames[0].position);
                datum.transform.localRotation = Quaternion.Inverse(donorRoot.rotation) * frames[0].rotation;
                meyerLights = S060MeyerLights.Bind(Parts.Car.transform, Parts.Body, datum.transform, meyerSupport.RearOffset, true);
            }
            // Pipe fitting owns a second shell copy. Unwind it before this
            // local clearance owner so stock always receives its original mesh.
            if (IsMeyer) meyerClearance = S060MeyerClearance.Apply(Parts.Body);
            if (IsMeyer) meyerFrontEquipment = S060MeyerFrontEquipment.Bind(Parts.Car.transform, Parts.Body);
            steamPipes = S060SteamPipes.Bind(Parts.Car.transform, liveFrames[1], liveFrames[0], Parts.Body);
            // Presentation is fully prepared before changing the engine model.
            simulation.Select(IsMeyer ? S060EngineMode.Meyer : S060EngineMode.Dockside);
            if (IsMeyer) capacity = S060MeyerCapacity.Apply(simulation.Flow, IsRestoring,
                Parts.Car.gameObject);
            // The native engine phases already drive each unit's absolute
            // animation/effects. Give those same engines their own wheel RPM,
            // grip and physical contact allocation after all fitting succeeds.
            var groups = new S060IndependentDrive.Definition[liveFrames.Count];
            for (int i = 0; i < groups.Length; i++)
            {
                var contacts = new int[Parts.Car.Bogies.Length];
                foreach (var wheel in powered)
                    if (wheel.wheelTransform.IsChildOf(liveFrames[i]))
                    {
                        int index = Array.FindIndex(Parts.Car.Bogies,
                            b => wheel.transform.IsChildOf(b.transform));
                        if (index < 0) throw new InvalidOperationException("Powered group has no native bogie contact.");
                        contacts[index]++;
                    }
                groups[i] = new S060IndependentDrive.Definition {
                    Slot = i == 0 ? simulation.Bank.PhysicalRear : simulation.Bank.PhysicalFront,
                    EngineRoot = liveFrames[i], Contacts = contacts,
                    Radius = IsMeyer ? S060Parts.DriverRadius : .5f,
                    Role = IsMeyer ? (i == 0 ? "LP/rear" : "HP/front") : (i == 0 ? "rear" : "front") };
            }
            independentDrive = S060IndependentDrive.Attach(Parts.Car, simulation, groups);
        }
        private void Hide(GameObject item) { hidden.Add((item, item.activeSelf)); item.SetActive(false); }
        protected override void ReleasePresentationMeshes() => meyerLights?.ReleaseMeshes();
        protected override void Undo()
        {
            var errors = new List<Exception>();
            bool Attempt(Action action) { try { action(); return true; } catch (Exception ex) { errors.Add(ex); return false; } }
            // Release RPM/force leases before changing the selected engines,
            // native axle registration, or deleting any frame/effect anchors.
            if (independentDrive != null && Attempt(() => {
                independentDrive.Release();
                if (Application.isPlaying) UnityEngine.Object.Destroy(independentDrive);
                else UnityEngine.Object.DestroyImmediate(independentDrive);
            })) independentDrive = null;
            // Keep failed owners reachable. In particular, do not destroy a
            // frame while a native lamp/cylinder/audio emitter still belongs
            // beneath it because restoring that owner failed.
            if (meyerLights != null && Attempt(meyerLights.Dispose)) meyerLights = null;
            if (meyerRunningGear != null && Attempt(meyerRunningGear.Dispose)) meyerRunningGear = null;
            if (bodywork != null && Attempt(bodywork.Dispose)) bodywork = null;
            if (nativeCylinders != null && Attempt(nativeCylinders.Dispose)) nativeCylinders = null;
            if (steamPipes != null && Attempt(steamPipes.Dispose)) steamPipes = null;
            foreach (var owner in meyerGearMounts.ToArray())
                if (Attempt(owner.Dispose)) meyerGearMounts.Remove(owner);
            if (steamPipes == null && meyerGearMounts.Count == 0 && meyerFrontMotion != null && Attempt(meyerFrontMotion.Dispose)) meyerFrontMotion = null;
            if (meyerFrontEquipment != null && Attempt(meyerFrontEquipment.Dispose)) meyerFrontEquipment = null;
            if (steamPipes == null && meyerClearance != null && Attempt(meyerClearance.Dispose)) meyerClearance = null;
            foreach (var owner in presentation.ToArray())
                if (Attempt(owner.Dispose)) presentation.Remove(owner);
            if (stockCocks != null && Attempt(stockCocks.Dispose)) stockCocks = null;
            if (capacity != null && Attempt(capacity.Dispose)) capacity = null;
            if (simulation != null) Attempt(() => simulation.Select(S060EngineMode.Simple));
            Attempt(Physics.Restore);
            foreach (var home in hidden.ToArray())
                if (Attempt(() => { if (home.part != null) home.part.SetActive(home.active); })) hidden.Remove(home);
            foreach (var home in renderers.ToArray())
                if (Attempt(() => { if (home.renderer != null) home.renderer.enabled = home.enabled; })) renderers.Remove(home);
            foreach (var home in readers.ToArray())
                if (Attempt(() => { if (home.component != null) home.component.enabled = home.enabled; })) readers.Remove(home);
            if (hidden.Count == 0 && nativeQuartering != null && Attempt(nativeQuartering.Dispose)) nativeQuartering = null;
            if (errors.Count == 0)
            {
                // Rear collision/contacts now belong to the frame's existing
                // bogie body. Release them before deleting their parent frame.
                if (meyerSupport != null) Attempt(meyerSupport.ReleaseCollision);
                if (errors.Count == 0)
                    foreach (var item in added.ToArray())
                        if (Attempt(() => { if (item != null) { item.SetActive(false); if (Application.isPlaying) UnityEngine.Object.Destroy(item); else UnityEngine.Object.DestroyImmediate(item); } })) added.Remove(item);
                if (errors.Count == 0 && meyerSupport != null && Attempt(meyerSupport.Restore)) meyerSupport = null;
            }
            Attempt(() => { Parts.FrontBogie.axles = Parts.RearBogie.axles = null; });
            if (errors.Count != 0) throw new AggregateException("S060 articulated cleanup failed", errors);
        }
    }
}
#endif

