#if B99_ALL_STANDARD
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RearrangedS282
{
    // Optional integration with S282 Model Tweaks 1.1.0 / DV Car Changer 1.5.4.
    // Keep Car Changer's normal config/body/save lifecycle, replacing only the
    // Delta-specific whole-bogie swap with presentation on our existing axles.
    internal static class DeltaTrailingTruckCompatibility
    {
        private const string DeltaId = "DeltaTrailingTruckAxles";
        private static FieldInfo configField, bodyField, carField;
        [ThreadStatic] private static object applyingDelta;
        internal static string Status { get; private set; }

        internal static void Register(Harmony harmony)
        {
            var tweakPatch = AccessTools.TypeByName("S282ModelTweaks.PatchAppliedChange");
            if (tweakPatch == null) return;
            var change = AccessTools.TypeByName("CarChanger.Game.Components.AppliedChange");
            var processor = AccessTools.TypeByName("CarChanger.Game.ComponentProcessor");
            var apply = AccessTools.Method(tweakPatch, "ApplyS282730A");
            var reset = AccessTools.Method(tweakPatch, "ReturnToDefault");
            var baseApply = AccessTools.Method(change, "ApplyS282730A");
            var baseReset = AccessTools.Method(change, "ReturnToDefault");
            var hide = AccessTools.Method(processor, "ProcessHideTransforms");
            configField = AccessTools.Field(change, "Config");
            bodyField = AccessTools.Field(change, "_body");
            carField = AccessTools.Field(change, "TrainCar");
            if (apply == null || reset == null || baseApply == null || baseReset == null || hide == null ||
                configField == null || bodyField == null || carField == null)
                throw new InvalidOperationException("Delta compatibility could not find the supported Model Tweaks / Car Changer APIs.");
            // Base apply runs first. Suppress only the Delta body's own automatic
            // hiding, so geometry snapshots never inherit temporary hidden flags.
            harmony.Patch(baseApply, prefix: Hook(nameof(BeginApply)), finalizer: Hook(nameof(EndApply)));
            harmony.Patch(hide, prefix: Hook(nameof(AllowNativeHiding)));
            harmony.Patch(apply, prefix: Hook(nameof(ApplyDelta)));
            harmony.Patch(baseReset, prefix: Hook(nameof(RemoveDelta)));
            harmony.Patch(reset, prefix: Hook(nameof(AllowNativeReset)));
            Status = "Delta trailing truck compatibility enabled (single trailing axle).";
            Main.Logger.Log(Status);
        }

        private static HarmonyMethod Hook(string name) => new HarmonyMethod(typeof(DeltaTrailingTruckCompatibility), name);

        private static bool IsDelta(object config)
        {
            if (config == null) return false;
            var type = config.GetType();
            var field = AccessTools.Field(type, "ModificationId");
            var id = field != null ? field.GetValue(config) : AccessTools.Property(type, "ModificationId")?.GetValue(config, null);
            return string.Equals(id as string, DeltaId, StringComparison.Ordinal);
        }

        private static bool IsDeltaChange(object change) => change != null && configField != null && IsDelta(configField.GetValue(change));

        private static void BeginApply(object __instance, object config, out object __state)
        {
            __state = applyingDelta;
            if (IsDelta(config)) applyingDelta = __instance;
        }

        private static Exception EndApply(object __state, Exception __exception)
        {
            applyingDelta = __state;
            return __exception;
        }

        private static bool AllowNativeHiding(GameObject gameObject)
        {
            return applyingDelta == null || (bodyField.GetValue(applyingDelta) as GameObject) != gameObject;
        }

        // These prefixes patch Model Tweaks' static Harmony callbacks, hence __0
        // is AppliedChange, not the instance of the method being patched.
        private static bool ApplyDelta(object __0, object __1)
        {
            if (!IsDelta(__1)) return true;
            var owner = __0 as Component;
            var car = carField.GetValue(__0) as TrainCar;
            var body = bodyField.GetValue(__0) as GameObject;
            try
            {
                if (owner == null || car == null || body == null)
                    throw new InvalidOperationException("Delta's applied body or owning S282 was not available.");
                var visuals = car.GetComponent<DeltaTrailingTruckVisuals>() ?? car.gameObject.AddComponent<DeltaTrailingTruckVisuals>();
                visuals.Bind(owner, body);
            }
            catch (Exception error)
            {
                // Never fall back to the incompatible physical bogie replacement.
                Clear(car, owner);
                if (body != null) body.SetActive(false);
                Status = "Delta visuals could not be applied; physical running gear was retained. See game log.";
                Main.Logger.LogException(Status, error);
            }
            return false;
        }

        private static void RemoveDelta(object __instance)
        {
            if (!IsDeltaChange(__instance)) return;
            var car = carField.GetValue(__instance) as TrainCar;
            Clear(car, __instance as Component);
            // ResetBody destroys at the end of the frame; hide its temporary
            // template now so reapply/removal cannot briefly show stock axles.
            var body = bodyField.GetValue(__instance) as GameObject;
            if (body != null) body.SetActive(false);
        }

        private static bool AllowNativeReset(object __0) => !IsDeltaChange(__0);

        internal static void Suspend(TrainCar car)
        {
            try { if (car != null) car.GetComponent<DeltaTrailingTruckVisuals>()?.Suspend(); }
            catch (Exception error) { Main.Logger.LogException("Could not suspend Delta visuals", error); }
        }

        internal static void Clear(TrainCar car, Component owner = null)
        {
            try { if (car != null) car.GetComponent<DeltaTrailingTruckVisuals>()?.Clear(owner); }
            catch (Exception error) { Main.Logger.LogException("Could not clean up Delta visuals", error); }
        }

        internal static bool IsPresentation(Transform candidate)
        {
            for (var current = candidate; current != null; current = current.parent)
                if (current.GetComponent<DeltaPresentationMarker>() != null) return true;
            return false;
        }

        internal static void Refresh(TrainCar car)
        {
            if (car == null) return;
            try { car.GetComponent<DeltaTrailingTruckVisuals>()?.Refresh(); }
            catch (Exception error)
            {
                Suspend(car);
                Status = "Delta graphics were suspended after a refresh error. See game log.";
                Main.Logger.LogException(Status, error);
            }
        }
    }

    internal sealed class DeltaPresentationMarker : MonoBehaviour { }
}
#endif
