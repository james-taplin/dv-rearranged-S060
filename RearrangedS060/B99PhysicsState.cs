using System;
using System.Linq;
using DV.ECS.Train;
using DV.Simulation.Cars;
using DV.Wheels;
using LocoSim.Implementations;
using LocoSim.Implementations.Wheels;

namespace RearrangedS060
{
    // One instance per selected locomotive. Never writes carLivery.parentType.
    internal sealed class B99PhysicsState
    {
        private readonly TrainCar car;
        private readonly SimController sim;
        private readonly AdhesionControllerSystem.StaticSharedAdhesionCalculationData originalShared;
        private readonly AdhesionControllerSystem.StaticAdhesionCalculationData originalFriction;
        private readonly PoweredWheel[] originalPowered;
        private readonly byte[] originalIndexes;
        private readonly float originalPort;
        private readonly float originalForceRadius;
        private readonly float originalRpmConstant;
        private readonly float originalSpeedConstant;
        private readonly PoweredWheelRotationViaAnimation[] rotations;
        private readonly float[] originalAnimationRadii;
        private readonly float[] originalCircumferences;
        private readonly Port poweredAxlesPort;
        private readonly LocoSim.Definitions.ConfigurablePortDefinition poweredDefinition;
        private readonly float originalDefinition;
        private readonly bool originalForceEnabled;
        private readonly Port originalEngineBrakingPort;

        public float DriverRadius => originalForceRadius;
        public PoweredWheel[] OriginalPowered => (PoweredWheel[])originalPowered.Clone();
        public int OriginalTotalAxles => originalShared.numOfAxles;

        // expectedPoweredAxles guards against selecting a locomotive whose drivetrain
        // another mod has already altered: the S060 ships with 3, the S282 with 4.
        public B99PhysicsState(TrainCar car, int expectedPoweredAxles)
        {
            this.car = car ?? throw new ArgumentNullException(nameof(car));
            if (car.entity == null || !car.entity.IsConverted)
                throw new InvalidOperationException("B99 entity conversion is still pending.");
            sim = car.SimController;
            if (sim?.simFlow == null || sim.drivingForce == null || sim.tractionPortsFeeder == null ||
                sim.wheelslipController == null || sim.poweredWheels?.poweredWheels == null)
                throw new InvalidOperationException("Native simulation is not initialized.");
            // TryGet also rejects selection before the native entity conversion finishes.
            if (!car.entity.TryGetComponentData<AdhesionControllerSystem.StaticSharedAdhesionCalculationData>().IsSome(out originalShared) ||
                !car.entity.TryGetComponentData<AdhesionControllerSystem.StaticAdhesionCalculationData>().IsSome(out originalFriction))
                throw new InvalidOperationException("B99 entity data is not initialized yet.");
            if (!sim.simFlow.TryGetPort(sim.wheelslipController.numberOfPoweredAxlesPortId, out poweredAxlesPort))
                throw new InvalidOperationException("Native powered axle port was not found.");
            originalPowered = (PoweredWheel[])sim.poweredWheels.poweredWheels.Clone();
            originalIndexes = originalPowered.Select(w => w.index).ToArray();
            originalPort = poweredAxlesPort.Value;
            poweredDefinition = car.transform.Find("[sim]/poweredAxles").GetComponent<LocoSim.Definitions.ConfigurablePortDefinition>();
            originalDefinition = poweredDefinition.value;
            originalForceEnabled = sim.drivingForce.enabled;
            originalEngineBrakingPort = sim.wheelslipController.engineBrakingActivePort;
            originalForceRadius = sim.drivingForce.wheelRadius;
            originalRpmConstant = sim.tractionPortsFeeder.speedMsToWheelRpmConst;
            originalSpeedConstant = sim.tractionPortsFeeder.wheelRpmToSpeedKmhConst;
            rotations = car.GetComponentsInChildren<PoweredWheelRotationViaAnimation>(true);
            originalAnimationRadii = rotations.Select(r => r.wheelRadius).ToArray();
            originalCircumferences = rotations.Select(r => r.wheelCircumference).ToArray();
            if (originalShared.numOfAxles != car.NumberOfAxles ||
                originalPowered.Length != expectedPoweredAxles || originalPort != expectedPoweredAxles)
                throw new InvalidOperationException("Expected an unmodified locomotive with " +
                    expectedPoweredAxles + " powered axles and consistent axle data; found " +
                    originalPowered.Length + " powered, port " + originalPort +
                    ", " + originalShared.numOfAxles + " axles.");
            ConventionalPhysics.RpmPerMetrePerSecond(originalForceRadius);
        }

        public void Apply(float radius, PoweredWheel[] powered, int totalAxles, float driverWeightFraction)
        {
            if (powered == null || powered.Any(w => w == null) || powered.Distinct().Count() != powered.Length)
                throw new ArgumentException("Powered wheel registration must contain distinct valid axles.");
            if (totalAxles <= 0 || totalAxles > byte.MaxValue || totalAxles < powered.Length ||
                (powered.Length == 0 && driverWeightFraction != 0f))
                throw new ArgumentException("Invalid physical axle or driver weight allocation.");
            float rpm = ConventionalPhysics.RpmPerMetrePerSecond(radius);
            float coefficient = powered.Length == 0 ? originalFriction.wheelslipFrictionCoef
                : ConventionalPhysics.FrictionForDriverWeight(originalFriction.wheelslipFrictionCoef,
                driverWeightFraction, powered.Length, totalAxles);
            var shared = originalShared;
            shared.numOfAxles = checked((byte)totalAxles);
            var friction = originalFriction;
            friction.wheelslipFrictionCoef = coefficient;
            // Keep wheel-slide friction separate from powered-wheel adhesion.
            car.entity.SetComponentData(shared);
            car.entity.SetComponentData(friction);
            sim.poweredWheels.poweredWheels = (PoweredWheel[])powered.Clone();
            for (byte i = 0; i < powered.Length; i++) powered[i].index = i;
            poweredAxlesPort.Value = powered.Length;
            poweredDefinition.value = powered.Length;
            sim.drivingForce.wheelRadius = radius;
            sim.tractionPortsFeeder.speedMsToWheelRpmConst = rpm;
            sim.tractionPortsFeeder.wheelRpmToSpeedKmhConst = 3.6f / rpm;
            foreach (var rotation in rotations)
            {
                rotation.wheelRadius = radius;
                rotation.wheelCircumference = 2f * (float)Math.PI * radius;
            }
            car.numberOfAxles = totalAxles;
            SetTractionAvailable(powered.Length != 0);
            RefreshForceLimit(totalAxles, powered.Length);
        }

        public void Restore()
        {
            car.entity.SetComponentData(originalShared);
            car.entity.SetComponentData(originalFriction);
            sim.poweredWheels.poweredWheels = (PoweredWheel[])originalPowered.Clone();
            for (int i = 0; i < originalPowered.Length; i++) originalPowered[i].index = originalIndexes[i];
            poweredAxlesPort.Value = originalPort;
            poweredDefinition.value = originalDefinition;
            sim.drivingForce.wheelRadius = originalForceRadius;
            sim.tractionPortsFeeder.speedMsToWheelRpmConst = originalRpmConstant;
            sim.tractionPortsFeeder.wheelRpmToSpeedKmhConst = originalSpeedConstant;
            for (int i = 0; i < rotations.Length; i++)
            {
                rotations[i].wheelRadius = originalAnimationRadii[i];
                rotations[i].wheelCircumference = originalCircumferences[i];
            }
            car.numberOfAxles = originalShared.numOfAxles;
            SetTractionAvailable(true);
            RefreshForceLimit(originalShared.numOfAxles, originalPowered.Length);
        }

        private void SetTractionAvailable(bool powered)
        {
            // Native DrivingForce does not check the axle count before producing
            // force. Disable that existing component for the zero-driver novelty;
            // leave the boiler, auxiliaries, brakes and rolling physics running.
            sim.drivingForce.enabled = powered && originalForceEnabled;
            if (!powered) sim.drivingForce.generatedForce = 0f;
            // The optional native engine-braking reader divides force by powered
            // axle count. An unpowered vehicle has no engine braking to distribute.
            sim.wheelslipController.engineBrakingActivePort = powered ? originalEngineBrakingPort : null;
        }

        private void RefreshForceLimit(int totalAxles, int poweredAxles)
        {
            // Copy_TrainCar_Data refreshes the input on the next native ECS tick. Clear
            // the previous arrangement's output now so no stale grip limit is reused.
            car.adhesionController.ResetState();
            var input = car.entity.GetComponentData<WheelslipControllerSystem.WheelslipInputData>();
            input.numberOfPoweredAxles = checked((ushort)poweredAxles);
            if (poweredAxles == 0) { input.generatedForce = 0f; input.isEngineBraking = false; }
            car.entity.SetComponentData(input);
            var calc = car.entity.GetComponentData<AdhesionControllerSystem.AdhesionCalculationData>();
            // The native system recomputes this from current mass each frame; do the
            // same during the transition, preserving other conditions and weather.
            calc.weightPerAxle = car.massController.WeightPerAxle;
            if (poweredAxles == 0) calc.engineBrakingForcePerAxle = 0f;
            car.entity.SetComponentData(calc);
        }
    }
}
