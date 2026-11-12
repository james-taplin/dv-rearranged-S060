#if B99_S060_ARTICULATED
using System;
using DV.ECS.Train;
using Unity.Entities;
using Unity.Transforms;

namespace RearrangedS060
{
    // Calculation data only. No TrainCar, managed WheelslipController, or
    // TrainCarPositionData: native calculation queries can process these while
    // managed vehicle copy/apply and position-monitor queries cannot own them.
    // The runtime owner must use the native conversion/ECB lifetime boundary;
    // this schema deliberately does not create or immediately destroy entities.
    internal static class S060NativeSlipEntitySchema
    {
        // Use only components already registered by the game. UMM loads this
        // assembly after TypeManager initialization; a mod-defined ECS tag would
        // fail in the player even though an early-loaded fixture can discover it.
        // The owning runtime retains its private, versioned wrapper identities.
        internal static EntityArchetype AdhesionCarrier(EntityManager manager) => manager.CreateArchetype(
            ComponentType.ReadWrite<LocalToWorld>(),
            ComponentType.ReadWrite<AdhesionControllerSystem.StaticSharedAdhesionCalculationData>(),
            ComponentType.ReadWrite<AdhesionControllerSystem.StaticAdhesionCalculationData>(),
            ComponentType.ReadWrite<AdhesionControllerSystem.AdhesionCalculationData>(),
            ComponentType.ReadWrite<AdhesionControllerSystem.WheelSlideData>(),
            ComponentType.ReadWrite<AdhesionControllerSystem.AdhesionWheelslipOutputData>(),
            ComponentType.ReadWrite<TrainCarPositionMonitorSystem.IsMovingTag>());

        internal static EntityArchetype SlipGroup(EntityManager manager) => manager.CreateArchetype(
            ComponentType.ReadWrite<WheelslipControllerSystem.WheelslipInputData>(),
            ComponentType.ReadWrite<WheelslipControllerSystem.WheelslipOutputData>(),
            ComponentType.ReadWrite<AdhesionControllerSystem.WheelSlideData>(),
            ComponentType.ReadWrite<AdhesionControllerSystem.StaticSharedAdhesionCalculationData>(),
            ComponentType.ReadWrite<AdhesionControllerSystem.AdhesionWheelslipOutputData>(),
            ComponentType.ReadWrite<TrainCarPositionMonitorSystem.IsMovingTag>());

        internal static void WriteGroupInputs(EntityManager manager, Entity group,
            WheelslipControllerSystem.WheelslipInputData input,
            AdhesionControllerSystem.StaticSharedAdhesionCalculationData shared,
            AdhesionControllerSystem.AdhesionWheelslipOutputData grip,
            AdhesionControllerSystem.WheelSlideData hostSlide)
        {
            if (!manager.Exists(group))
                throw new InvalidOperationException("Slip input belongs to a retired entity.");
            ValidateGroupInputs(input, shared, grip, hostSlide);
            manager.SetComponentData(group, input);
            manager.SetComponentData(group, shared);
            manager.SetComponentData(group, grip);
            manager.SetComponentData(group, hostSlide);
            // Never rewrite WheelslipOutputData here: native smoothing state is
            // persistent across input updates and must remain group-local.
        }

        internal static void ValidateGroupInputs(WheelslipControllerSystem.WheelslipInputData input,
            AdhesionControllerSystem.StaticSharedAdhesionCalculationData shared,
            AdhesionControllerSystem.AdhesionWheelslipOutputData grip,
            AdhesionControllerSystem.WheelSlideData hostSlide)
        {
            if (input.numberOfPoweredAxles == 0 || shared.numOfAxles == 0 || shared.bogieCount == 0 ||
                input.numberOfPoweredAxles > shared.numOfAxles)
                throw new ArgumentException("Invalid group/native axle allocation.");
            if (!Finite(input.generatedForce) || !Nonnegative(input.brakingForce) ||
                !Nonnegative(input.wheelslipToAdhesionDrop) || !Nonnegative(input.maxWheelslipRpm) ||
                !Nonnegative(grip.wheelslipForceLimitPerAxle) || !Nonnegative(hostSlide.wheelSlide) ||
                hostSlide.wheelSlide > 1f || !Finite(hostSlide.wheelSlideSmoothRefVel))
                throw new ArgumentException("Invalid/non-finite group input cannot enter native slip calculations.");
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Nonnegative(float value) => Finite(value) && value >= 0f;
    }
}
#endif
