#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using DV.ECS.Train;
using DV.Utils;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace RearrangedS060
{
    // Owns native calculation helpers only. Vehicle forces and steam/animation
    // ports remain the responsibility of the per-locomotive coordinator.
    internal sealed class S060NativeSlipRuntimeState : IDisposable
    {
        internal static readonly List<S060NativeSlipRuntimeState> States = new List<S060NativeSlipRuntimeState>();
        internal readonly World World;
        private readonly EntityManager manager;
        private readonly GameObject root;
        private readonly DVConvertToEntity carrier;
        private readonly DVConvertToEntity[] groups;
        private readonly WheelslipControllerSystem.WheelslipInputData[] inputs;
        private readonly AdhesionControllerSystem.StaticSharedAdhesionCalculationData[] shared;
        private readonly AdhesionControllerSystem.WheelSlideData[] slides;
        private readonly WheelslipControllerSystem.WheelslipOutputData[] outputs;
        private readonly bool[] written;
        private readonly float[] gripMultipliers;
        private bool disposed, carrierWritten, bridged;

        internal event Action PrepareInputs;
        internal event Action OutputsReady;
        internal event Action<Exception> Failed;
        internal bool HasCompletedSample { get; private set; }
        internal float BasePerAxleGrip { get; private set; }
        internal int SampleNumber { get; private set; }
        internal bool Ready => !disposed && World.IsCreated && carrier.IsConverted && groups.All(g => g != null && g.IsConverted);

        internal S060NativeSlipRuntimeState(Transform parent, TrainCar car, int groupCount, int generation)
            : this(parent, ResolveManager(car), groupCount, generation) { }

        private static EntityManager ResolveManager(TrainCar car)
        {
            if (car?.entity == null || !car.entity.IsConverted) throw new InvalidOperationException("Car entity conversion is pending.");
            return car.entity.EntityManager;
        }

        // The manager overload also permits the force-free native World fixture
        // to exercise the exact wrapper lifetime without manufacturing a TrainCar.
        internal S060NativeSlipRuntimeState(Transform parent, EntityManager entityManager, int groupCount, int generation)
        {
            if (parent == null || groupCount < 1 || generation < 1)
                throw new ArgumentException("Native slip helpers require a converted car and a valid activation.");
            manager = entityManager;
            World = manager.World;
            if (DVConvertToEntity.ConvertSystem == null || DVConvertToEntity.ConvertSystem.World != World)
                throw new InvalidOperationException("Native conversion system is not ready for this car's World.");
            inputs = new WheelslipControllerSystem.WheelslipInputData[groupCount];
            shared = new AdhesionControllerSystem.StaticSharedAdhesionCalculationData[groupCount];
            slides = new AdhesionControllerSystem.WheelSlideData[groupCount];
            outputs = new WheelslipControllerSystem.WheelslipOutputData[groupCount];
            written = new bool[groupCount]; groups = new DVConvertToEntity[groupCount]; gripMultipliers = new float[groupCount];
            root = new GameObject("[Rearranged native group calculations " + generation + "]");
            root.transform.SetParent(parent, false);
            try
            {
                carrier = Create("adhesion carrier", S060NativeSlipEntitySchema.AdhesionCarrier(manager));
                for (int i = 0; i < groupCount; i++) groups[i] = Create("slip group " + i, S060NativeSlipEntitySchema.SlipGroup(manager));
                var simulation = World.GetOrCreateSystem<SimulationSystemGroup>();
                simulation.AddSystemToUpdateList(World.GetOrCreateSystem<S060NativeSlipPrepareSystem>());
                simulation.AddSystemToUpdateList(World.GetOrCreateSystem<S060NativeSlipBridgeSystem>());
                simulation.AddSystemToUpdateList(World.GetOrCreateSystem<S060NativeSlipSampleSystem>());
                simulation.SortSystems();
                States.Add(this);
            }
            catch { Dispose(); throw; }
        }

        private DVConvertToEntity Create(string name, EntityArchetype archetype)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            var wrapper = child.AddComponent<DVConvertToEntity>();
            wrapper.Initialize(archetype);
            return wrapper;
        }

        internal void SetCarrier(AdhesionControllerSystem.StaticSharedAdhesionCalculationData common,
            AdhesionControllerSystem.StaticAdhesionCalculationData friction,
            AdhesionControllerSystem.AdhesionCalculationData calculation, LocalToWorld pose)
        {
            if (!Ready) throw new InvalidOperationException("Native helpers are not converted yet.");
            manager.SetComponentData(carrier.Entity, common);
            manager.SetComponentData(carrier.Entity, friction);
            manager.SetComponentData(carrier.Entity, calculation);
            manager.SetComponentData(carrier.Entity, pose);
            carrierWritten = true;
        }

        internal void SetGroup(int index, WheelslipControllerSystem.WheelslipInputData input,
            AdhesionControllerSystem.StaticSharedAdhesionCalculationData common,
            AdhesionControllerSystem.WheelSlideData hostSlide, float diagnosticGripMultiplier = 1f)
        {
            if (!Ready || index < 0 || index >= groups.Length) throw new InvalidOperationException("Native group is not ready.");
            if (!(diagnosticGripMultiplier > 0f && diagnosticGripMultiplier <= 1f) || float.IsNaN(diagnosticGripMultiplier))
                throw new ArgumentOutOfRangeException(nameof(diagnosticGripMultiplier));
            inputs[index] = input; shared[index] = common; slides[index] = hostSlide; written[index] = true;
            gripMultipliers[index] = diagnosticGripMultiplier;
        }

        internal WheelslipControllerSystem.WheelslipOutputData GetOutput(int index)
        {
            // Prepare consumes the preceding native slip for its adhesion curve;
            // HasCompletedSample is cleared until this frame's Sample finishes.
            if (disposed || SampleNumber == 0) return default;
            return outputs[index];
        }

        internal void Prepare()
        {
            HasCompletedSample = bridged = carrierWritten = false;
            Array.Clear(written, 0, written.Length);
            if (!Ready) return;
            manager.CompleteAllJobs();
            PrepareInputs?.Invoke();
        }

        internal void Bridge()
        {
            if (!Ready || !carrierWritten || written.Any(value => !value)) return;
            manager.CompleteAllJobs();
            var grip = manager.GetComponentData<AdhesionControllerSystem.AdhesionWheelslipOutputData>(carrier.Entity);
            BasePerAxleGrip = grip.wheelslipForceLimitPerAxle;
            for (int i = 0; i < groups.Length; i++)
            {
                var groupGrip = grip;
                groupGrip.wheelslipForceLimitPerAxle *= gripMultipliers[i];
                S060NativeSlipEntitySchema.WriteGroupInputs(manager, groups[i].Entity, inputs[i], shared[i], groupGrip, slides[i]);
            }
            bridged = true;
        }

        internal void Sample()
        {
            if (!Ready || !bridged) return;
            manager.CompleteAllJobs();
            for (int i = 0; i < groups.Length; i++) outputs[i] = manager.GetComponentData<WheelslipControllerSystem.WheelslipOutputData>(groups[i].Entity);
            HasCompletedSample = true;
            SampleNumber++;
            OutputsReady?.Invoke();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; HasCompletedSample = false;
            PrepareInputs = null; OutputsReady = null; Failed = null;
            States.Remove(this);
            // Native wrappers queue Disable/Destroy before BeginSimulation ECB.
            // Never immediately destroy an entity while the slip job's previous
            // Begin/End buffers can still contain operations for it.
            if (root != null) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
        }

        internal static void ForWorld(World world, Action<S060NativeSlipRuntimeState> action)
        {
            // Callbacks can retire their owner. Advance only if that entry is
            // still present, without allocating a fleet snapshot three times
            // per native simulation step.
            for (int i = 0, remaining = States.Count; i < States.Count && remaining-- > 0;)
            {
                var state = States[i];
                if (state.World == world && !state.disposed)
                    try { action(state); }
                    catch (Exception error)
                    {
                        var callback = state.Failed;
                        state.Dispose();
                        callback?.Invoke(error);
                    }
                if (i < States.Count && ReferenceEquals(States[i], state)) i++;
            }
        }
    }

    [DisableAutoCreation, UpdateInGroup(typeof(SimulationSystemGroup)), UpdateBefore(typeof(AdhesionControllerSystem))]
    internal sealed class S060NativeSlipPrepareSystem : SystemBase
    {
        protected override void OnUpdate() => S060NativeSlipRuntimeState.ForWorld(World, state => state.Prepare());
    }
    [DisableAutoCreation, UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(AdhesionControllerSystem)), UpdateBefore(typeof(WheelslipControllerSystem))]
    internal sealed class S060NativeSlipBridgeSystem : SystemBase
    {
        protected override void OnUpdate() => S060NativeSlipRuntimeState.ForWorld(World, state => state.Bridge());
    }
    [DisableAutoCreation, UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(WheelslipControllerSystem))]
    internal sealed class S060NativeSlipSampleSystem : SystemBase
    {
        protected override void OnUpdate() => S060NativeSlipRuntimeState.ForWorld(World, state => state.Sample());
    }
}
#endif
