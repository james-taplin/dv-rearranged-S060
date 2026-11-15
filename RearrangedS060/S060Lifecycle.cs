#if B99_S060_FUNCTIONAL
using System;
using System.Collections;
using DV.Simulation.Cars;
using HarmonyLib;
using LocoMeshSplitter.MeshLoaders;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // Adapted from the S282 fork's native simCarState lifecycle. Owns only
    // this instance; never changes shared liveries, prefabs or S282 save keys.
    internal sealed class S060Lifecycle : MonoBehaviour
    {
        private static readonly System.Random Random = new System.Random();
        private readonly S060SavedState state = new S060SavedState();
        private TrainCar car;
        private Coroutine pending;
        private bool waitingReported;
        private bool restoringSave;
        internal string Status { get; private set; }
        internal bool HasPending => state.Pending.HasValue;

        internal static void Register(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(CarStarted)).Patch();
            harmony.CreateClassProcessor(typeof(NewLogicCar)).Patch();
            harmony.CreateClassProcessor(typeof(SavingState)).Patch();
            harmony.CreateClassProcessor(typeof(LoadingState)).Patch();
            harmony.CreateClassProcessor(typeof(ReturningToPool)).Patch();
            harmony.CreateClassProcessor(typeof(PreparingForDestroy)).Patch();
        }

        private static S060Lifecycle For(TrainCar candidate)
        {
            if (!S060Parts.IsS060(candidate) || candidate.logicCar == null || string.IsNullOrEmpty(candidate.CarGUID)) return null;
            var owner = candidate.GetComponent<S060Lifecycle>() ?? candidate.gameObject.AddComponent<S060Lifecycle>();
            owner.car = candidate;
            owner.state.BeginIdentity(candidate.CarGUID);
            return owner;
        }

        private static void Schedule(TrainCar candidate)
        {
            if (S060MultiplayerSync.DeferAutomatic(candidate)) return;
            var owner = For(candidate);
            if (owner == null) return;
            owner.state.SelectInitial(Main.Options.PickInitial(Random));
            owner.StartPending();
        }

        internal static bool TryApply(TrainCar candidate, S060Selection selection, out string message) =>
            S060MultiplayerSync.RequestChange(candidate, selection, out message);

        internal static bool ApplyLocal(TrainCar candidate, S060Selection selection, out string message, bool restoring = false)
        {
            if (!S060MultiplayerSync.BeforeSwitch(candidate)) { message = "Only the host can apply arrangements."; return false; }
            var owner = For(candidate);
            if (owner == null) { message = "Select an initialized S060."; return false; }
            try
            {
                S060Conversion.SwitchTo(candidate, selection, restoring);
                owner.CancelPending();
                owner.state.RecordApplied(selection);
                owner.restoringSave = false;
                S060MultiplayerSync.OnApplied(candidate, selection);
                owner.Status = message = candidate.ID + ": " + selection.Description;
                Main.Logger.Log(message);
                return true;
            }
            catch (Exception ex)
            {
                S060MultiplayerSync.OnSwitchFailed(candidate);
                owner.Status = message = "Not applied: " + ex.Message;
                Main.Logger.LogException("S060 selection refused", ex);
                return false;
            }
        }

        private void StartPending()
        {
            if (!state.Pending.HasValue || car == null || S060MultiplayerSync.DeferAutomatic(car)) return;
            if (state.Pending.Value.IsStock && S060Conversion.Current(car) == null)
            {
                state.RecordApplied(S060Selection.Stock);
                S060MultiplayerSync.OnApplied(car, S060Selection.Stock);
                Status = "Stock 0-6-0";
                return;
            }
            if (pending == null && isActiveAndEnabled) pending = StartCoroutine(ApplyWhenReady());
        }

        private void CancelPending()
        {
            if (pending != null) StopCoroutine(pending);
            pending = null;
        }

        private void OnEnable() => StartPending();
        private void OnDisable() => CancelPending();
        private void OnDestroy()
        {
            if (!ReferenceEquals(car, null))
                try { S060Conversion.ForgetDestroyed(car); }
                catch (Exception ex) { Main.Logger.LogException("S060 destroyed resource cleanup failed", ex); }
        }

        [HarmonyPatch(typeof(TrainCar), nameof(TrainCar.PrepareForDestroy))]
        private static class PreparingForDestroy
        {
            private static void Prefix(TrainCar __instance)
            {
                if (!S060Parts.IsS060(__instance)) return;
                try { S060Conversion.Release(__instance); }
                catch (Exception ex)
                {
                    Main.Logger.LogException("S060 pre-destruction restore failed; releasing owned resources", ex);
                    try { S060Conversion.ForgetDestroyed(__instance); }
                    catch (Exception cleanup) { Main.Logger.LogException("S060 final mesh cleanup failed", cleanup); }
                }
            }
        }

        private bool Ready()
        {
            if (car.entity == null || !car.entity.IsConverted || !car.AreBogiesFullyInitialized() ||
                car.GetComponent<S060MeshLoader>() == null || car.transform.Find("LocoS060_Body/DrivingMechanism_R") == null)
                return false;
            if (restoringSave && S060Conversion.Current(car) == null) return !car.derailed && !car.isExploded;
            try { S060Conversion.RequireStationary(car); return true; }
            catch (InvalidOperationException) { return false; }
        }

        private IEnumerator ApplyWhenReady()
        {
            // Let native restoration and MeshSplitter Start finish first.
            yield return null;
            while (car != null && car.logicCar != null && state.Guid == car.CarGUID && state.Pending.HasValue)
            {
                if (S060MultiplayerSync.DeferAutomatic(car)) break;
                if (Ready())
                {
                    var selection = state.Pending.Value;
                    try
                    {
                        S060Conversion.SwitchTo(car, selection, restoringSave);
                        state.RecordApplied(selection);
                        S060MultiplayerSync.OnApplied(car, selection);
                        restoringSave = false;
                        Status = "Restored " + selection.Description;
                        Main.Logger.Log(car.ID + ": " + Status);
                    }
                    catch (Exception ex)
                    {
                        Status = "Saved selection could not be applied; retry or restore stock with the radio.";
                        Main.Logger.LogException(car.ID + ": " + Status, ex);
                        // Retain intent in the save, but never loop geometry failures.
                    }
                    break;
                }
                Status = "Saved/spawn selection pending: stop and close the regulator.";
                if (!waitingReported) { Main.Logger.Log(car.ID + ": " + Status); waitingReported = true; }
                yield return new WaitForSecondsRealtime(.5f);
            }
            pending = null;
        }

        [HarmonyPatch(typeof(TrainCar), "Start")]
        private static class CarStarted
        {
            [HarmonyAfter("LocoMeshSplitter", "Gauge")]
            private static void Postfix(TrainCar __instance) => Schedule(__instance);
        }
        [HarmonyPatch(typeof(TrainCar), nameof(TrainCar.InitializeNewLogicCar))]
        private static class NewLogicCar
        {
            private static void Postfix(TrainCar __instance) => Schedule(__instance);
        }
        [HarmonyPatch(typeof(SimCarStateSave), nameof(SimCarStateSave.GetStateSaveData))]
        private static class SavingState
        {
            private static void Postfix(SimCarStateSave __instance, JObject __result)
            {
                if (__result == null) return;
                var owner = For(__instance.GetComponent<TrainCar>());
                if (owner == null) return;
                owner.state.SelectInitial(S060Selection.Stock);
#if B99_S060_ARTICULATED
                owner.state.RecordWheelStates(S060WheelCondition.For(owner.car).Capture());
#endif
                __result[S060SavedState.SaveKey] = owner.state.Save();
            }
        }
        [HarmonyPatch(typeof(CarsSaveManager), nameof(CarsSaveManager.RestoreCarState))]
        private static class LoadingState
        {
#if B99_S060_ARTICULATED
            private static void Prefix(TrainCar spawnedCar, JObject simCarState)
            {
                if (!S060Parts.IsS060(spawnedCar) || S060MultiplayerSync.DeferAutomatic(spawnedCar)) return;
                var saved = new S060SavedState();
                if (!saved.Load(simCarState?[S060SavedState.SaveKey]) || saved.Pending?.Arrangement != S060Arrangement.Meyer) return;
                // Restore the longer supports/coupling anchor before native
                // RestoreCarConnections, which runs after all car states load.
                try { S060MeyerSupport.For(spawnedCar).Apply(true); }
                catch (Exception ex) { Main.Logger.LogException("Saved Meyer supports could not be prepared", ex); }
            }
#endif
            private static void Postfix(TrainCar spawnedCar, JObject simCarState)
            {
                if (S060MultiplayerSync.DeferAutomatic(spawnedCar)) return;
                var owner = For(spawnedCar);
                if (owner == null) return;
                owner.CancelPending();
                owner.waitingReported = false;
                owner.restoringSave = true;
                if (!owner.state.Load(simCarState?[S060SavedState.SaveKey]))
                {
                    owner.Status = "Unsupported save data retained; choose an arrangement to replace it.";
                    Main.Logger.Warning(spawnedCar.ID + ": " + owner.Status);
                }
#if B99_S060_ARTICULATED
                if (owner.state.WheelStates != null) S060WheelCondition.For(spawnedCar).Load(owner.state.WheelStates);
#endif
                owner.StartPending();
            }
        }
        [HarmonyPatch(typeof(TrainCar), nameof(TrainCar.ReturnCarToPool))]
        private static class ReturningToPool
        {
            private static bool Prefix(TrainCar __instance)
            {
                if (!S060Parts.IsS060(__instance)) return true;
                S060MultiplayerSync.Forget(__instance);
                var owner = __instance.GetComponent<S060Lifecycle>();
                if (owner != null)
                {
                    owner.CancelPending();
                    owner.state.Reset();
                    owner.waitingReported = false;
                    owner.restoringSave = false;
                    owner.Status = null;
                }
                try
                {
                    S060Conversion.Release(__instance);
#if B99_S060_ARTICULATED
                    __instance.GetComponent<S060MeyerSupport>()?.Restore();
                    __instance.GetComponent<S060WheelCondition>()?.ResetIdentity();
#endif
                    return true;
                }
                catch (Exception ex)
                {
                    Main.Logger.LogException("S060 reset failed; discarding this deleted car instead of pooling modified gear", ex);
                    try
                    {
                        if (__instance.interior != null) UnityEngine.Object.Destroy(__instance.interior.gameObject);
                        __instance.PrepareForDestroy();
                    }
                    catch (Exception cleanup) { Main.Logger.LogException("Native cleanup failed for discarded S060", cleanup); }
                    finally
                    {
                        S060Conversion.ForgetDestroyed(__instance);
                        UnityEngine.Object.Destroy(__instance.gameObject);
                    }
                    return false;
                }
            }
        }
    }
}
#endif
