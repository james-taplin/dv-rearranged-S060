#if B99_S060_ARTICULATED
using System;
using System.Linq;
using HarmonyLib;
using LocoSim.Implementations.Wheels;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS060
{
    internal sealed class S060WheelCondition : MonoBehaviour
    {
        private PoweredWheel[] stock, current;
        private int[] slots;
        private int[] states = new int[5];
        internal static void Register(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(NativeSaving)).Patch();
            harmony.CreateClassProcessor(typeof(RepairingAll)).Patch();
        }
        internal static S060WheelCondition For(TrainCar car, PoweredWheel[] native = null)
        {
            if (!S060Parts.IsS060(car)) return null;
            var owner = car.GetComponent<S060WheelCondition>() ?? car.gameObject.AddComponent<S060WheelCondition>();
            if (owner.stock == null)
            {
                native = native ?? car.SimController.poweredWheels.poweredWheels;
                if (native.Length != 3) throw new InvalidOperationException("Native wheel identities must be captured before conversion.");
                owner.stock = (PoweredWheel[])native.Clone(); owner.current = (PoweredWheel[])native.Clone(); owner.slots = new[] { 0, 1, 2 };
                owner.Sync();
            }
            return owner;
        }
        private void Sync()
        {
            if (current == null) return;
            for (int i = 0; i < current.Length; i++) if (current[i] != null) states[slots[i]] = (int)current[i].state;
        }
        internal int[] Capture() { Sync(); return states.Take(current?.Length == 5 || states[4] != 0 ? 5 : 4).ToArray(); }
        internal void Load(int[] saved)
        {
            if (!S060WheelSlots.Valid(saved)) throw new InvalidOperationException("Invalid saved S060 wheel condition.");
            states = new int[5]; Array.Copy(saved, states, saved.Length); ApplyStates();
        }
        private void ApplyStates()
        {
            for (int i = 0; i < current.Length; i++) current[i].state = (PoweredWheel.State)states[slots[i]];
            // Keep hidden original wheels current too, for stock restoration and
            // the native save's three canonical wheel indexes.
            for (int i = 0; i < stock.Length; i++) stock[i].state = (PoweredWheel.State)states[i];
        }
        internal void Bind( PoweredWheel[] wheels)
        {
            Sync();
            int[] originalIndexes = wheels.Select(w => Array.FindIndex(stock, source => ReferenceEquals(source, w))).ToArray();
            slots = S060WheelSlots.Map(originalIndexes); current = (PoweredWheel[])wheels.Clone(); ApplyStates();
        }
        internal void ResetIdentity() { stock = current = null; slots = null; states = new int[5]; }
        [HarmonyPatch(typeof(PoweredWheelsManager), nameof(PoweredWheelsManager.GetSaveStateData))]
        private static class NativeSaving
        {
            private static bool Prefix(PoweredWheelsManager __instance, ref JObject __result)
            {
                var car = __instance.GetComponent<TrainCar>();
                if (!S060Parts.IsS060(car)) return true;
                var owner = car.GetComponent<S060WheelCondition>();
                if (owner?.stock == null) return true;
                owner.Sync();
                var broken = Enumerable.Range(0, 3).Where(i => owner.states[i] == (int)PoweredWheel.State.BROKEN).ToArray();
                // Native restoration precedes geometry: never hand its original
                // three-wheel manager a fourth index. The mod saves every owned wheel separately.
                __result = broken.Length == 0 ? null : new JObject { ["deadIndices"] = new JArray(broken) };
                return false;
            }
        }
        [HarmonyPatch(typeof(PoweredWheelsManager), nameof(PoweredWheelsManager.RepairAllPoweredWheels))]
        private static class RepairingAll
        {
            private static void Postfix(PoweredWheelsManager __instance)
            {
                var owner = __instance.GetComponent<S060WheelCondition>();
                if (owner?.stock == null) return;
                owner.Sync();
                for (int i = 0; i < owner.states.Length; i++)
                    if (owner.states[i] == (int)PoweredWheel.State.BROKEN) owner.states[i] = (int)PoweredWheel.State.CUT_OUT;
                owner.ApplyStates();
            }
        }
    }
}
#endif
