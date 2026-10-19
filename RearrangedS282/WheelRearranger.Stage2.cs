using System;
using System.Linq;
using DV.ThingTypes;
using DV.Wheels;
using UnityEngine;

namespace RearrangedS282
{
    internal partial class WheelRearranger
    {
        private B99PhysicsState stage2Physics;
        private Bogie stage2RearBogie;
        private Transform stage2OriginalRearAxle;
        private bool stage2RearWasActive;
        private Transform stage2TrailingA;
        private Transform stage2TrailingB;
        private WheelRotationViaCode stage2RollingRotation;
        private Transform[] stage2OriginalRollingTransforms;
        private Bogie.AxleInfo[] stage2OriginalRearAxles;
        public bool Stage2Initialized => stage2Physics != null;

        internal bool TrySwitchStage2(int id)
        {
#if B99_ALL_STANDARD
            return TrySwitchConventional(id);
#else
            if (id != (int)WheelArrangementType.s282 && id != (int)WheelArrangementType.s284)
            {
                Main.Logger.Warning("Stage 2 supports only stock 2-8-2 and 2-8-4.");
                return false;
            }
            if (skipWheelArrangementChange) return false;
            loco = GetComponent<TrainCar>();
            if (loco == null || loco.carType != TrainCarType.LocoSteamHeavy || loco.derailed ||
                Mathf.Abs(loco.GetForwardSpeed()) > 0.01f)
            {
                Main.Logger.Warning("Select a stationary, railed S282 before changing its wheel arrangement.");
                return false;
            }
            var sim = loco.SimController;
            if (sim?.simFlow == null || !sim.simFlow.TryGetPort("throttle.EXT_IN", out var throttle) || throttle.Value > 0.001f)
            {
                Main.Logger.Warning("Close the regulator before changing the wheel arrangement.");
                return false;
            }
            if (sim.drivingForce == null || Mathf.Abs(sim.drivingForce.generatedForce) > 500f ||
                sim.wheelslipController == null || sim.wheelslipController.IsWheelslipping || loco.adhesionController.IsWheelSliding)
            {
                Main.Logger.Warning("Wait for driving force and wheel slip to settle before changing the arrangement.");
                return false;
            }
            // Selecting stock before any conversion must be a complete no-op.
            if (!Stage2Initialized && id == (int)WheelArrangementType.s282) return true;
            skipWheelArrangementChange = true;
            try
            {
                if (!Stage2Initialized) InitializeStage2();
                bool berkshire = id == (int)WheelArrangementType.s284;
                SetStage2Geometry(berkshire);
                if (berkshire)
                {
                    var powered = stage2Physics.OriginalPowered;
                    int total = loco.Bogies.Sum(b => b.Axles.Length);
                    // Explicit approximation: equal axle loads, as in the original
                    // rearranger. No invented prototype weight-on-drivers figure.
                    float fraction = (float)powered.Length / total;
                    stage2Physics.Apply(stage2Physics.DriverRadius, powered, total, fraction);
                }
                else stage2Physics.Restore();
                currentWA = (WheelArrangementType)id;
                LogStage2State("applied");
                return true;
            }
            catch (Exception ex)
            {
                Main.Logger.LogException("Stage 2 conversion failed; restoring the captured stock state", ex);
                if (Stage2Initialized)
                {
                    try
                    {
                        SetStage2Geometry(false);
                        stage2Physics.Restore();
                        currentWA = WheelArrangementType.s282;
                    }
                    catch (Exception restoreError) { Main.Logger.LogException("Stock restoration failed", restoreError); }
                }
                return false;
            }
            finally { skipWheelArrangementChange = false; }
#endif
        }

        private void InitializeStage2()
        {
            var physics = new B99PhysicsState(loco);
            if (physics.OriginalTotalAxles != 6)
                throw new InvalidOperationException("Stage 2 requires the stock six-axle S282 geometry.");
            var powered = physics.OriginalPowered;
            stage2RearBogie = loco.RearBogie;
            stage2OriginalRearAxles = stage2RearBogie.Axles;
            var rearUnpowered = stage2OriginalRearAxles.Where(a => !powered.Any(w => w.transform == a.transform)).ToArray();
            var frontUnpowered = loco.FrontBogie.Axles.Where(a => !powered.Any(w => w.transform == a.transform)).ToArray();
            if (rearUnpowered.Length != 1 || frontUnpowered.Length != 1)
                throw new InvalidOperationException("Could not identify the stock leading and trailing unpowered axles.");
            stage2OriginalRearAxle = rearUnpowered[0].transform;
            stage2RearWasActive = stage2OriginalRearAxle.gameObject.activeSelf;
            Transform source = frontUnpowered[0].transform;
            stage2RollingRotation = loco.GetComponentsInChildren<WheelRotationViaCode>(true)
                .Single(r => r.transformsToRotate != null && r.transformsToRotate.Contains(source));
            stage2OriginalRollingTransforms = stage2RollingRotation.transformsToRotate;
            try
            {
                // Reuse the original rearranger's two small trailing-wheel positions,
                // without initializing its unrelated rod/body/duplex machinery.
                stage2TrailingA = Instantiate(source.gameObject, stage2OriginalRearAxle.parent).transform;
                stage2TrailingA.name = "[axle] rearranged trailing A";
                stage2TrailingA.gameObject.SetActive(false);
                stage2TrailingB = Instantiate(source.gameObject, stage2OriginalRearAxle.parent).transform;
                stage2TrailingB.name = "[axle] rearranged trailing B";
                stage2TrailingB.gameObject.SetActive(false);
                stage2TrailingA.localPosition = new Vector3(0f, source.localPosition.y, -2.52f);
                stage2TrailingB.localPosition = new Vector3(0f, source.localPosition.y, -1.32f);
                // Derailroader separates unpowered rotation from driver slip. These
                // wheels share the native leading-wheel rotation/radius configuration.
                stage2Physics = physics;
            }
            catch
            {
                if (stage2TrailingA != null) Destroy(stage2TrailingA.gameObject);
                if (stage2TrailingB != null) Destroy(stage2TrailingB.gameObject);
                stage2RearBogie.axles = stage2OriginalRearAxles;
                throw;
            }
        }

        private void SetStage2Geometry(bool berkshire)
        {
            stage2OriginalRearAxle.gameObject.SetActive(berkshire ? false : stage2RearWasActive);
            stage2TrailingA.gameObject.SetActive(berkshire);
            stage2TrailingB.gameObject.SetActive(berkshire);
            stage2RollingRotation.transformsToRotate = berkshire
                ? stage2OriginalRollingTransforms.Concat(new[] { stage2TrailingA, stage2TrailingB }).ToArray()
                : stage2OriginalRollingTransforms;
            stage2RearBogie.axles = berkshire
                ? stage2OriginalRearAxles.Where(a => a.transform != stage2OriginalRearAxle)
                    .Concat(new[] { new Bogie.AxleInfo(stage2TrailingA), new Bogie.AxleInfo(stage2TrailingB) }).ToArray()
                : stage2OriginalRearAxles;
            loco.numberOfAxles = loco.Bogies.Sum(b => b.Axles.Length);
        }

        internal void LogStage2State(string action)
        {
            var sim = loco.SimController;
            var shared = loco.entity.GetComponentData<DV.ECS.Train.AdhesionControllerSystem.StaticSharedAdhesionCalculationData>();
            var friction = loco.entity.GetComponentData<DV.ECS.Train.AdhesionControllerSystem.StaticAdhesionCalculationData>();
            Main.Logger.Log($"Stage2 {action}: {loco.ID} {currentWA}; axles car/entity={loco.NumberOfAxles}/{shared.numOfAxles}; " +
                $"powered manager/port={sim.poweredWheels.poweredWheels.Length}/{sim.wheelslipController.NumberOfPoweredAxles}; " +
                $"radius={sim.drivingForce.wheelRadius:R}; slipCoef={friction.wheelslipFrictionCoef:R}; mass={loco.massController.CarMass:R}");
        }
    }
}
