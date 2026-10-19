#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DV.ThingTypes;
using DV.Wheels;
using LocoSim.Implementations.Wheels;
using LocoMeshSplitter.MeshLoaders;
using UnityEngine;

namespace RearrangedS282
{
    internal partial class WheelRearranger
    {
        private GeometryState stockGeometry, preparedGeometry;
        private Transform[] generatedRoots = Array.Empty<Transform>();
        private bool conventionalFault;
        private ArticulatedSimulation articulatedSimulation;
        private B99ExplosionMaterials explosionMaterials;
        internal int ExplosionMaterialRendererCount => explosionMaterials?.Count ?? 0;
        internal bool ConventionalSetupBlocked => conventionalFault;
        internal string LastConventionalError { get; private set; }

        private bool TrySwitchConventional(int id)
        {
            if (!WheelArrangement.IsSupportedId(id) || skipWheelArrangementChange || conventionalFault) return false;
            loco = GetComponent<TrainCar>();
            var sim = loco?.SimController;
            if (loco == null || loco.carType != TrainCarType.LocoSteamHeavy || loco.derailed || loco.isExploded ||
                Mathf.Abs(loco.GetForwardSpeed()) > .01f || sim?.simFlow == null ||
                !sim.simFlow.TryGetPort("throttle.EXT_IN", out var throttle) || throttle.Value > .001f ||
                sim.drivingForce == null || Mathf.Abs(sim.drivingForce.generatedForce) > 500f ||
                sim.wheelslipController == null || sim.wheelslipController.IsWheelslipping ||
                loco.adhesionController.IsWheelSliding)
            {
                Main.Logger.Warning("Use a repaired, stationary S282 with its regulator closed and wheel slip/driving force settled before switching.");
                return false;
            }
            if (stockGeometry == null && id == 10) return true;
            LastConventionalError = null;
            skipWheelArrangementChange = true;
            try
            {
                DeltaTrailingTruckCompatibility.Suspend(loco);
                if (stockGeometry == null)
                {
                    splitS282A = GetComponent<S282AMeshLoader>();
                    if (splitS282A == null || transform.Find("LocoS282A_Body/Static_LOD0/s282a_split_body(Clone)") == null)
                        throw new InvalidOperationException("Loco Mesh Splitter has not prepared this S282 yet.");
                    // Extract rods before modifying the locomotive, using the original mod's helper.
                    var meshes = MeshFinder.Instance;
                    stage2Physics = new B99PhysicsState(loco);
                    stockGeometry = new GeometryState(loco);
                    explosionMaterials = new B99ExplosionMaterials(loco,
                        transform.Find("LocoS282A_Body/MovingParts_LOD0"));
                    var originalTransforms = new HashSet<Transform>(GetComponentsInChildren<Transform>(true));
                    try
                    {
                        SetupBody(); SetupLubrication(); SetupAir();
                        if (loco.AreExternalInteractablesLoaded) LoadExternalInteractables(loco.loadedExternalInteractables);
                        SetupFrontBogie(); SetupRearBogie(); SetupDrivers();
                        SetupPacificHudsonValveGear();
                        SetupArticulatedGeometry();
                        preparedGeometry = new GeometryState(loco);
                    }
                    finally
                    {
                        generatedRoots = GetComponentsInChildren<Transform>(true)
                            .Where(t => !originalTransforms.Contains(t) && originalTransforms.Contains(t.parent)).ToArray();
                    }
                }
                if (id == 10) RestoreConventionalStock();
                else
                {
                    ArticulatedEffects.Deinit(loco);
                    articulatedSimulation?.Restore();
                    SuspendArticulatedGeometry();
                    SuspendAdditionalGeometry();
            Suspend4144Geometry();
                    preparedGeometry.Restore();
                    currentWA = WheelArrangementType.s282;
                    if (WheelArrangement.IsArticulatedId(id))
                    {
                        articulatedSimulation = ArticulatedSimulation.Ensure(loco);
                        if (id == (int)WheelArrangementType.s2662) ApplyMalletGeometry();
                        else if (id == (int)WheelArrangementType.s22222) ApplyTriplexGeometry(true);
                        else if (id == (int)WheelArrangementType.s02220) ApplyTriplexGeometry(false);
                        else ApplyArticulatedGeometry(id);
                    }
                    else
                    {
                        if (id == (int)WheelArrangementType.s4144) Apply4144Geometry();
                        else
                        {
                            ApplyLegacyGeometry(id);
                            if ((id == (int)WheelArrangementType.s462 || id == (int)WheelArrangementType.s464) &&
                                Main.settings.polishedPacificHudsonValveGear)
                                ApplyPacificHudsonValveGear();
                            AlignAdditionalPoweredAxles(id);
                        }
                    }
                    foreach (var bogie in loco.Bogies)
                        bogie.axles = Enumerable.Range(0, bogie.transform.Find("bogie_car").childCount)
                            .Select(i => bogie.transform.Find("bogie_car").GetChild(i))
                            .Where(t => t.name.StartsWith("[axle]", StringComparison.Ordinal) && t.gameObject.activeInHierarchy)
                            .Select(t => new Bogie.AxleInfo(t)).ToArray();
                    var powered = new[] { driveAxle1, driveAxle2, driveAxle3, driveAxle4, driveAxle5, driveAxle6, driveAxle7 }
                        .Where(t => t != null && t.gameObject.activeInHierarchy).Select(t => t.GetComponent<PoweredWheel>()).ToArray();
                    int total = loco.Bogies.Sum(b => b.Axles.Length);
                    var wheels = WheelArrangement.WheelArrangementNames[id].Split(' ')[0].Split('-').Select(int.Parse).ToArray();
                    int expectedPowered = WheelArrangement.IsArticulatedId(id) ? wheels.Skip(1).Take(wheels.Length - 2).Sum() / 2 : wheels[1] / 2;
                    if (powered.Length != expectedPowered || total != wheels.Sum() / 2)
                        throw new InvalidOperationException("Active physical axles do not match the selected arrangement.");
                    // Equal axle loads, with driven-axle loads capped for small coupled groups.
                    var groups = WheelArrangement.IsArticulatedId(id)
                        ? wheels.Skip(1).Take(wheels.Length - 2).Select(w => w / 2).ToArray() : new[] { wheels[1] / 2 };
                    float mass = loco.massController.CarMass;
                    float fraction = ConventionalPhysics.DriverWeightFraction(groups, total, mass);
                    stage2Physics.Apply(sim.drivingForce.wheelRadius, powered, total, fraction);
                    Main.Logger.Log($"Driver weight: {fraction * mass / 1000f:F1} t on {powered.Length} driven axles " +
                        $"(equal share {(float)powered.Length / total * mass / 1000f:F1} t of {mass / 1000f:F0} t).");
                    currentWA = (WheelArrangementType)id;
                    if (WheelArrangement.IsArticulatedId(id)) articulatedSimulation.Apply(id);
                    RefreshConventionalRollingAudio();
                }
                UpdateExternalControlPositions();
                B99Oilcups.Bind(loco, loco.loadedExternalInteractables);
                ArticulatedEffects.Refresh(loco, WheelArrangement.IsArticulatedId(id));
                // Includes inactive conventional LODs and lazily created rear
                // engines. Exclude other integrations' separately owned effects.
                explosionMaterials?.Refresh(GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => !DeltaTrailingTruckCompatibility.IsPresentation(r.transform) &&
                        !ArticulatedEffects.IsOwnedPresentation(r.transform)));
                GetComponent<LMSCustomizationPlacementMeshes>()?.ReloadAllGadgetMeshTransforms();
                LogStage2State("arrangement applied");
                return true;
            }
            catch (Exception ex)
            {
                LastConventionalError = ex.Message;
                Main.Logger.LogException("Conventional trial conversion failed; restoring stock", ex);
                try { if (stockGeometry != null) RestoreConventionalStock(); }
                catch (Exception restore) { Main.Logger.LogException("Stock restoration failed", restore); }
                // An incomplete setup must never be repeated over partially generated geometry.
                conventionalFault = stockGeometry != null && preparedGeometry == null;
                return false;
            }
            finally { skipWheelArrangementChange = false; DeltaTrailingTruckCompatibility.Refresh(loco); }
        }

        private void RestoreConventionalStock()
        {
            ArticulatedEffects.Deinit(loco);
            articulatedSimulation?.Restore();
            SuspendArticulatedGeometry();
            SuspendAdditionalGeometry();
            Suspend4144Geometry();
            stockGeometry.Restore();
            // Restore original parent ownership before deactivating generated frames.
            foreach (var t in generatedRoots) if (t != null) t.gameObject.SetActive(false);
            stage2Physics.Restore();
            RefreshConventionalRollingAudio();
            currentWA = WheelArrangementType.s282;
            UpdateExternalControlPositions();
            derailModifier = 0f;
        }

        // Native pooling can reuse this GameObject for a different locomotive.
        // Restore its per-car stock snapshot before native reset/pooling begins.
        internal bool ResetConventionalForPool()
        {
            try
            {
                loco = GetComponent<TrainCar>();
                DeltaTrailingTruckCompatibility.Clear(loco);
                if (stockGeometry != null) RestoreConventionalStock();
                currentWA = WheelArrangementType.s282;
                return true;
            }
            catch (Exception ex)
            {
                Main.Logger.LogException("Could not restore stock running gear before pooling this S282", ex);
                return false;
            }
        }

        private void OnDestroy()
        {
            ArticulatedEffects.Deinit(loco);
            DisposeArticulatedGeometry();
            DisposePacificHudsonValveGear();
            DisposeAdditionalGeometry();
            // Unity may destroy the TrainCar component before this owner. Mesh
            // assets still need explicit cleanup even when the car compares null.
            if (loco == null) return;
            loco.ExternalInteractableLoaded -= LoadExternalInteractables;
            loco.ExternalInteractableAboutToBeUnloaded -= UnloadExternalInteractables;
        }

        // Restore only properties the legacy geometry path edits. Never rewind the
        // locomotive or bogie rigidbodies to their location at initialization.
        private sealed class GeometryState
        {
            private readonly List<Action> restore = new List<Action>();
            public GeometryState(TrainCar car)
            {
                CaptureFrontCouplingMounts(car);
                foreach (var t in car.GetComponentsInChildren<Transform>(true))
                {
                    if (DeltaTrailingTruckCompatibility.IsPresentation(t) || ArticulatedEffects.IsOwnedPresentation(t)) continue;
                    var p = t.localPosition; var q = t.localRotation; var s = t.localScale;
                    var parent = t.parent; int sibling = t.GetSiblingIndex();
                    var name = t.name; bool active = t.gameObject.activeSelf;
                    bool pose = t != car.transform && t.GetComponent<Rigidbody>() == null;
                    restore.Add(() => { if (t == null) return; t.name = name;
                        if (pose) {
                            if (t.parent != parent) t.SetParent(parent, false);
                            t.SetSiblingIndex(sibling);
                            t.localPosition = p; t.localRotation = q; t.localScale = s;
                        }
                        t.gameObject.SetActive(active); });
                }
                foreach (var f in car.GetComponentsInChildren<MeshFilter>(true))
                { if (DeltaTrailingTruckCompatibility.IsPresentation(f.transform) || ArticulatedEffects.IsOwnedPresentation(f.transform)) continue;
                    var mesh = f.sharedMesh; restore.Add(() => { if (f != null) f.sharedMesh = mesh; }); }
                foreach (var r in car.GetComponentsInChildren<Renderer>(true))
                { if (DeltaTrailingTruckCompatibility.IsPresentation(r.transform) || ArticulatedEffects.IsOwnedPresentation(r.transform)) continue;
                    bool enabled = r.enabled; restore.Add(() => { if (r != null) r.enabled = enabled; }); }
                foreach (var a in car.GetComponentsInChildren<Animator>(true))
                { if (DeltaTrailingTruckCompatibility.IsPresentation(a.transform) || ArticulatedEffects.IsOwnedPresentation(a.transform)) continue;
                    var controller = a.runtimeAnimatorController; restore.Add(() => { if (a != null) a.runtimeAnimatorController = controller; }); }
                foreach (var j in car.GetComponentsInChildren<Joint>(true))
                { if (DeltaTrailingTruckCompatibility.IsPresentation(j.transform)) continue;
                    var anchor = j.connectedAnchor; bool auto = j.autoConfigureConnectedAnchor;
                    restore.Add(() => { if (j != null) { j.autoConfigureConnectedAnchor = false; j.connectedAnchor = anchor; j.autoConfigureConnectedAnchor = auto; } }); }
                foreach (var b in car.Bogies)
                { var axles = b.Axles; restore.Add(() => b.axles = axles); }
                foreach (var wheel in car.GetComponentsInChildren<PoweredWheel>(true))
                {
                    var wheelTransform = wheel.wheelTransform; var axis = wheel.localRotationAxis;
                    restore.Add(() => { if (wheel != null) { wheel.wheelTransform = wheelTransform; wheel.localRotationAxis = axis; } });
                }
                // Native component-owned arrays replaced by the original setup.
                var fieldNames = new HashSet<string> { "transformsToRotate", "brakeRenderers", "wheelSparks", "sparkAnchors", "sparks" };
                foreach (var component in car.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null))
                foreach (var field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (DeltaTrailingTruckCompatibility.IsPresentation(component.transform) || ArticulatedEffects.IsOwnedPresentation(component.transform)) continue;
                    if (!fieldNames.Contains(field.Name) || field.IsInitOnly) continue;
                    var value = field.GetValue(component);
                    restore.Add(() => { if (component != null) field.SetValue(component, value); });
                }
            }
            private void CaptureFrontCouplingMounts(TrainCar car)
            {
                // LMS MovePilot moves these two mount roots along with the pilot.
                // DV parents them under car.interior, a separate scene root, so
                // the car hierarchy snapshot below cannot restore them. Restore
                // only the mount positions, never native chain/hose IK or state.
                var visual = car.frontCoupler?.visualCoupler;
                foreach (var mount in new[] { visual?.chain, visual?.hoses })
                {
                    if (mount == null) continue;
                    var position = mount.localPosition;
                    restore.Add(() => { if (mount != null) mount.localPosition = position; });
                }
            }
            public void Restore() { foreach (var action in restore) action(); }
        }
    }
}
#endif
