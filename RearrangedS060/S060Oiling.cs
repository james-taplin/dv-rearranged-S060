#if B99_S060_FUNCTIONAL
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using DV.Simulation.Cars;
using HarmonyLib;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;

namespace RearrangedS060
{
    // Existing S282 pattern: extend native manual oiling before flow creation.
    // Six original guide/crosshead/big-end circuits keep their IDs and quantities;
    // four additional big ends per side cover the five-axle layout.
    internal static class S060Oiling
    {
        internal const int NativePoints = S060OilingTopology.NativePoints,
            ExtraPerSide = S060OilingTopology.ExtraPerSide, TotalPoints = S060OilingTopology.TotalPoints;
        private sealed class State
        {
            internal bool[] Active;
            internal readonly float[,] Before = new float[TotalPoints, 5];
            internal int Depth;
        }
        private static readonly ConditionalWeakTable<ManualOilingPoints, State> States = new ConditionalWeakTable<ManualOilingPoints, State>();
        internal static void Register(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(SimController), nameof(SimController.Initialize)),
                prefix: new HarmonyMethod(typeof(S060Oiling), nameof(Prepare)), postfix: new HarmonyMethod(typeof(S060Oiling), nameof(Initialized)));
            harmony.Patch(AccessTools.Method(typeof(ManualOilingPoints), nameof(ManualOilingPoints.Tick)),
                prefix: new HarmonyMethod(typeof(S060Oiling), nameof(BeforeTick)), finalizer: new HarmonyMethod(typeof(S060Oiling), nameof(FinishTick)));
            harmony.Patch(AccessTools.Method(typeof(ManualOilingPoints), "OnSpecialRequest"),
                prefix: new HarmonyMethod(typeof(S060Oiling), nameof(BeforeTick)), finalizer: new HarmonyMethod(typeof(S060Oiling), nameof(FinishTick)));
            harmony.Patch(AccessTools.Method(typeof(ManualOilingPoints), nameof(ManualOilingPoints.SetSaveStateData)),
                prefix: new HarmonyMethod(typeof(S060Oiling), nameof(FitSave)));
        }
        internal static ManualOilingPointsDefinition Definition(TrainCar car) => car.GetComponentsInChildren<ManualOilingPointsDefinition>(true).FirstOrDefault();
        internal static ManualOilingPoints Points(SimulationFlow flow) => flow?.OrderedSimComps?.OfType<ManualOilingPoints>().FirstOrDefault();
        private static void Prepare(TrainCar trainCar)
        {
            if (!S060Parts.IsS060(trainCar)) return;
            var definition = Definition(trainCar);
            if (definition == null) throw new InvalidOperationException("Native S060 manual oiling definition missing.");
            ExtendDefinition(definition);
        }
        internal static void ExtendDefinition(ManualOilingPointsDefinition definition)
        {
            if (definition.oilingPoints.Length == TotalPoints) return;
            if (definition.oilingPoints.Length != NativePoints && definition.oilingPoints.Length != 12) throw new InvalidOperationException("Unexpected S060 manual oiling point count.");
            var template = definition.oilingPoints[2];
            PortDefinition Copy(PortDefinition port, int index) => new PortDefinition(port.type, port.valueType, port.ID + "_RS060BIG" + index);
            definition.oilingPoints = definition.oilingPoints.Concat(Enumerable.Range(definition.oilingPoints.Length, TotalPoints - definition.oilingPoints.Length).Select(i => new ManualOilingPointsDefinition.OilingPointDefinition
            {
                oilLevelReadOut = Copy(template.oilLevelReadOut, i), oilLevelNormalizedReadOut = Copy(template.oilLevelNormalizedReadOut, i),
                pointDoorExtIn = Copy(template.pointDoorExtIn, i), refillExtIn = Copy(template.refillExtIn, i),
                refillingFlowNormalizedReadOut = Copy(template.refillingFlowNormalizedReadOut, i)
            })).ToArray();
        }
        private static void Initialized(SimController __instance, TrainCar trainCar)
        {
            if (!S060Parts.IsS060(trainCar)) return;
            var points = Points(__instance.simFlow);
            if (points?.oilingPoints.Length != TotalPoints) throw new InvalidOperationException("S060 oiling circuit extension failed.");
            // New reservoirs start full once per newly created flow. The native
            // save loader runs later and restores all previously saved values.
            if (S060MultiplayerSync.IsSimulationAuthority)
            for (int i = NativePoints; i < TotalPoints; i++)
            { points.oilingPoints[i].oilLevelReadOut.Value = points.capacity; points.oilingPoints[i].oilLevelNormalizedReadOut.Value = 1; }
            // The stock locomotive also has three driven axles. Its two extra
            // big ends per side must work before external interactables stream.
            Select(points, S060Selection.Stock);
            S060OilCups.Attach(trainCar);
        }
        internal static void Select(TrainCar car, S060Selection selection) =>
            Select(Points(car?.SimController?.simFlow), selection);
        internal static void Select(ManualOilingPoints points, S060Selection selection) =>
            ConfigureActive(points, S060OilingTopology.ActiveMask(selection));
        internal static void ConfigureActive(ManualOilingPoints points, bool[] active)
        {
            if (points == null || active == null || active.Length != TotalPoints) return;
            var state = States.GetValue(points, key => new State());
            if (state.Depth != 0) throw new InvalidOperationException("Cannot change oil cup layout during a native oiling tick.");
            state.Active = (bool[])active.Clone();
        }
        // Mask only dormant circuits during the native tick. Restore their exact
        // oil, lid and refill state afterwards: selecting another layout is not a refill.
        internal static void BeforeTick(ManualOilingPoints __instance)
        {
            if (!S060MultiplayerSync.IsSimulationAuthority || !States.TryGetValue(__instance, out var state) || state.Active == null) return;
            if (state.Depth++ != 0) return;
            for (int i = 0; i < TotalPoints; i++)
            {
                if (state.Active[i]) continue;
                var p = __instance.oilingPoints[i];
                state.Before[i, 0] = p.oilLevelReadOut.Value; state.Before[i, 1] = p.oilLevelNormalizedReadOut.Value;
                state.Before[i, 2] = p.pointDoorExtIn.Value; state.Before[i, 3] = p.refillExtIn.Value; state.Before[i, 4] = p.refillingFlowNormalizedReadOut.Value;
                p.oilLevelReadOut.Value = __instance.capacity; p.oilLevelNormalizedReadOut.Value = 1;
                p.pointDoorExtIn.Value = p.refillExtIn.Value = p.refillingFlowNormalizedReadOut.Value = 0;
            }
        }
        internal static void AfterTick(ManualOilingPoints __instance)
        {
            if (!States.TryGetValue(__instance, out var state) || state.Depth == 0) return;
            if (--state.Depth != 0) return;
            for (int i = 0; i < TotalPoints; i++)
            {
                if (state.Active[i]) continue; var p = __instance.oilingPoints[i];
                p.oilLevelReadOut.Value = state.Before[i, 0]; p.oilLevelNormalizedReadOut.Value = state.Before[i, 1]; p.pointDoorExtIn.Value = state.Before[i, 2];
                p.refillExtIn.Value = state.Before[i, 3]; p.refillingFlowNormalizedReadOut.Value = state.Before[i, 4];
            }

        }
        private static Exception FinishTick(ManualOilingPoints __instance, Exception __exception)
        { AfterTick(__instance); return __exception; }
        internal static JArray FitSaved(JArray saved, int count)
        {
            // Only the shipped six/twelve -> fourteen migrations are known.
            // Leave future, foreign and malformed arrays with the native loader;
            // never truncate them or replace unknown entries with full reservoirs.
            if (count != TotalPoints || saved == null || (saved.Count != NativePoints && saved.Count != 12) ||
                saved.Any(point => !(point is JObject))) return saved;
            var fitted = (JArray)saved.DeepClone();
            while (fitted.Count < count) fitted.Add(new JObject { ["open"] = false, ["oilLevel"] = 1f });
            return fitted;
        }
        internal static void FitSave(ManualOilingPoints __instance, ref JObject __0)
        {
            if (__0 == null || __instance?.oilingPoints.Length != TotalPoints || !States.TryGetValue(__instance, out _)) return;
            if (!(__0["oilingPoints"] is JArray saved)) return;
            var fitted = FitSaved(saved, TotalPoints);
            if (ReferenceEquals(saved, fitted)) return;
            var component = (JObject)__0.DeepClone(); component["oilingPoints"] = fitted; __0 = component;
        }
    }
}
#endif
