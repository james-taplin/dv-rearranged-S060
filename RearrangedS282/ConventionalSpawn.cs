#if B99_ALL_STANDARD
using System;
using System.Collections;
using DV.Simulation.Cars;
using DV.ThingTypes;
using HarmonyLib;
using LocoMeshSplitter.MeshLoaders;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS282
{
    // Per-instance lifecycle only: shared loco definitions and prefabs are never
    // edited. Native simCarState follows the car through ordinary and unique-car
    // save/restore, including a pending choice that could not safely be applied.
    internal sealed class ConventionalSpawn : MonoBehaviour
    {
        private static readonly System.Random Random = new System.Random();
        private readonly ConventionalArrangementState state = new ConventionalArrangementState();
        private TrainCar car;
        private Coroutine pending;
        private bool waitingReported;

        internal static void RegisterPatches(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(TrainCarStarted)).Patch();
            harmony.CreateClassProcessor(typeof(NewLogicCar)).Patch();
            harmony.CreateClassProcessor(typeof(ReturningToPool)).Patch();
            harmony.CreateClassProcessor(typeof(SavingCarState)).Patch();
            harmony.CreateClassProcessor(typeof(LoadingCarState)).Patch();
        }

        private static bool IsS282(TrainCar candidate) => candidate != null &&
            candidate.carType == TrainCarType.LocoSteamHeavy && candidate.logicCar != null;

        private static ConventionalSpawn For(TrainCar candidate)
        {
            if (!IsS282(candidate)) return null;
            var owner = candidate.GetComponent<ConventionalSpawn>() ?? candidate.gameObject.AddComponent<ConventionalSpawn>();
            owner.car = candidate;
            if (owner.state.BeginIdentity(candidate.CarGUID)) owner.waitingReported = false;
            return owner;
        }

        private static void Schedule(TrainCar candidate)
        {
            var owner = For(candidate);
            if (owner == null) return;
            owner.state.SelectInitial(Main.settings, Random);
            owner.StartPending();
        }

        internal static void RecordApplied(TrainCar candidate, int id)
        {
            var owner = For(candidate);
            if (owner == null || !WheelArrangement.IsSupportedId(id)) return;
            owner.state.RecordApplied(id);
            owner.CancelPending();
        }

        private void StartPending()
        {
            // Newly instantiated/native-pooled stock needs no geometry or sim
            // edit, even if loaded with its regulator open or while derailed.
            if (state.PendingId == (int)WheelArrangementType.s282 && car != null &&
                (car.GetComponent<WheelRearranger>() == null || car.GetComponent<WheelRearranger>().currentWA == WheelArrangementType.s282))
            {
                state.RecordApplied((int)WheelArrangementType.s282);
                return;
            }
            if (pending == null && state.PendingId.HasValue && isActiveAndEnabled)
                pending = StartCoroutine(ApplyWhenReady());
        }

        private void CancelPending()
        {
            if (pending != null) StopCoroutine(pending);
            pending = null;
        }

        private void OnEnable() { StartPending(); }
        private void OnDisable() { CancelPending(); }

        private IEnumerator ApplyWhenReady()
        {
            // Native restore and Mesh Splitter's Start both complete before the
            // first attempt, including loading cars whose logic already exists.
            yield return null;
            while (car != null && car.logicCar != null &&
                string.Equals(state.Guid, car.CarGUID, StringComparison.Ordinal) && state.PendingId.HasValue)
            {
                if (!Stage2RuntimeChecks.Running && ReadyToSwitch())
                {
                    int id = state.PendingId.Value;
                    var rearranger = car.GetComponent<WheelRearranger>() ?? car.gameObject.AddComponent<WheelRearranger>();
                    if (rearranger.TrySwitchStage2(id))
                    {
                        state.RecordApplied(id);
                        Main.Logger.Log($"Restored/assigned {car.ID}: {WheelArrangement.WheelArrangementNames[id]}.");
                    }
                    else
                    {
                        // Conversion failures are not initialization waits. Keep
                        // the saved intent but never repeat a failed geometry edit.
                        Main.Logger.Warning($"Automatic arrangement for {car.ID} was refused; keeping the saved choice for reload. Use the radio to select stock or retry.");
                    }
                    break;
                }
                if (!waitingReported && car.entity != null && car.entity.IsConverted)
                {
                    Main.Logger.Log($"Arrangement for {car.ID} is pending: waiting for initialized, stationary running gear and a closed regulator.");
                    waitingReported = true;
                }
                yield return new WaitForSecondsRealtime(.5f);
            }
            pending = null;
        }

        private bool ReadyToSwitch()
        {
            var sim = car.SimController;
            int id = state.PendingId.Value;
            return !car.derailed && !car.isExploded && car.entity != null && car.entity.IsConverted &&
                car.AreBogiesFullyInitialized() && Mathf.Abs(car.GetForwardSpeed()) <= .01f &&
                sim?.simFlow != null && sim.simFlow.TryGetPort("throttle.EXT_IN", out var throttle) && throttle.Value <= .001f &&
                sim.drivingForce != null && Mathf.Abs(sim.drivingForce.generatedForce) <= 500f &&
                sim.wheelslipController != null && !sim.wheelslipController.IsWheelslipping &&
                car.adhesionController != null && !car.adhesionController.IsWheelSliding &&
                (id == (int)WheelArrangementType.s282 || (car.GetComponent<S282AMeshLoader>() != null &&
                    car.transform.Find("LocoS282A_Body/Static_LOD0/s282a_split_body(Clone)") != null));
        }

        [HarmonyPatch(typeof(TrainCar), "Start")]
        private static class TrainCarStarted
        {
            [HarmonyAfter("LocoMeshSplitter", "Gauge")]
            private static void Postfix(TrainCar __instance) { Schedule(__instance); }
        }

        [HarmonyPatch(typeof(TrainCar), nameof(TrainCar.InitializeNewLogicCar))]
        private static class NewLogicCar
        {
            private static void Postfix(TrainCar __instance) { Schedule(__instance); }
        }

        [HarmonyPatch(typeof(TrainCar), nameof(TrainCar.ReturnCarToPool))]
        private static class ReturningToPool
        {
            private static bool Prefix(TrainCar __instance)
            {
                if (__instance == null || __instance.carType != TrainCarType.LocoSteamHeavy) return true;
                var owner = __instance.GetComponent<ConventionalSpawn>();
                if (owner != null)
                {
                    owner.CancelPending();
                    owner.state.Reset();
                    owner.waitingReported = false;
                }
                var rearranger = __instance.GetComponent<WheelRearranger>();
                if (rearranger != null && !rearranger.ResetConventionalForPool())
                {
                    // CarSpawner has already removed this car from AllCars and
                    // called InteriorOnDestroy. Follow its unpooled teardown
                    // instead of either reusing faulty gear or orphaning a car.
                    Main.Logger.Error("S282 stock restoration failed; destroying this deleted car instead of returning faulty running gear to the pool.");
                    try
                    {
                        if (__instance.interior != null) UnityEngine.Object.Destroy(__instance.interior.gameObject);
                        __instance.PrepareForDestroy();
                    }
                    catch (Exception ex)
                    {
                        Main.Logger.LogException("Native cleanup failed for the S282 being removed from the pool", ex);
                    }
                    finally { UnityEngine.Object.Destroy(__instance.gameObject); }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SimCarStateSave), nameof(SimCarStateSave.GetStateSaveData))]
        private static class SavingCarState
        {
            private static void Postfix(SimCarStateSave __instance, JObject __result)
            {
                if (__result == null) return;
                var owner = For(__instance.GetComponent<TrainCar>());
                if (owner != null) __result[ConventionalArrangementState.SaveKey] = owner.state.SavePayload();
            }
        }

        [HarmonyPatch(typeof(CarsSaveManager), nameof(CarsSaveManager.RestoreCarState))]
        private static class LoadingCarState
        {
            private static void Postfix(TrainCar spawnedCar, JObject simCarState)
            {
                var owner = For(spawnedCar);
                if (owner == null) return;
                owner.CancelPending();
                if (!owner.state.LoadPayload(simCarState?[ConventionalArrangementState.SaveKey]))
                    Main.Logger.Warning($"{owner.car.ID}: unsupported arrangement save data retained; stock running gear will be used until a radio selection replaces it.");
                owner.state.SelectInitial(Main.settings, Random);
                owner.StartPending();
            }
        }
    }
}
#endif
