using System;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS060
{
    internal sealed class MeyerCycleOwner
    {
        internal MeyerCompoundCycle Cycle;
        internal MeyerNativeStage Hp, Lp;
    }

    internal sealed class MeyerStageDefinition : ReciprocatingSteamEngineDefinition
    {
        internal bool HighPressure;
        internal MeyerCycleOwner Owner;
        public override SimComponent InstantiateImplementation()
        {
            var stage = new MeyerNativeStage(this);
            if (HighPressure) Owner.Hp = stage; else Owner.Lp = stage;
            return stage;
        }
    }

    // Keep DV's native port contract, crank events, condensate, lubrication,
    // damage and repair. Replace the atmospheric-only chest/work calculation
    // for these two Meyer instances; no patch to the global engine Tick.
    internal sealed class MeyerNativeStage : ReciprocatingSteamEngine
    {
        private readonly bool highPressure;
        private readonly MeyerCycleOwner owner;
        private bool motionPrepared;
        private bool hasMechanicalState;
        private bool seedAdmissions;
        private int inletMask;
        internal MeyerNativeStage(MeyerStageDefinition definition) : base(definition)
        {
            highPressure = definition.HighPressure; owner = definition.Owner;
            (highPressure ? owner.Cycle.Hp : owner.Cycle.Lp).MechanicalTorqueRequest = NativeTorqueRequest;
        }

        private void PrepareMotion(float delta)
        {
            prevCrankRotation = crankRotation;
            crankRotation = Mathf.Repeat(crankRotation + crankRpm.Value / 60 * delta, 1);
            isReverserForward = reverserControl.Value >= 0;
            cutoff = Mathf.Lerp(minCutoff, maxCutoff, Mathf.Abs(reverserControl.Value));
            condensationExpansionRatio = CondensationExpansionRatio();
            inletMask = 0;
            for (int i = 0; i < numCylinders; i++)
            {
                cylinderData[i].SimulateCylinder(pistonRotationLead * i);
                if (cylinderData[i].isInletValveOpen) inletMask |= 1 << i;
            }
            motionPrepared = true;
        }

        private double NativeTorqueRequest(double chestPressure, double backPressure)
        {
            // The native piston works against 1 bar. Present the pressure
            // difference plus that reference, so HP still works against the
            // real receiver and LP against atmosphere; no second boiler feed.
            float effectiveBar = (float)(1 + Math.Max(0, chestPressure - backPressure) / 100000);
            if (seedAdmissions)
            {
                // Older saves used mean work and have no trapped-cylinder
                // pressure. Seed only the mechanical cache from their retained
                // circuit, using DV's native half-stroke sign convention.
                for (int i = 0; i < numCylinders; i++)
                    cylinderData[i].admissionPressure = (isReverserForward ^ cylinderData[i].isCrankAboveHorizontal)
                        ? -effectiveBar : effectiveBar;
                seedAdmissions = false;
            }
            hasMechanicalState = true;
            float rpm = Mathf.Abs(crankRpm.Value);
            if (rpm < RPM_FOR_LERP_START) return SimulateInstantaneousTorque(effectiveBar);
            float mean = SimulateMeanTorque(effectiveBar);
            return rpm >= RPM_FOR_LERP_END ? mean : Mathf.Lerp(SimulateInstantaneousTorque(effectiveBar), mean,
                Mathf.InverseLerp(RPM_FOR_LERP_START, RPM_FOR_LERP_END, rpm));
        }

        internal void RephaseMechanics()
        {
            // Arrangement selection changes the crank phase. The old phase's
            // trapped-pressure signs must not follow it into the new geometry.
            seedAdmissions = (highPressure ? owner.Cycle.Hp : owner.Cycle.Lp).Chest.Mass > 1e-12;
            if (!seedAdmissions)
                for (int i = 0; i < numCylinders; i++) cylinderData[i].admissionPressure = 1;
            hasMechanicalState = false;
            motionPrepared = false;
        }

        public override void Tick(float delta)
        {
            if (delta == 0) return;
            if (highPressure)
            {
                // Both native crank states must precede their work requests.
                // LP publishes later in the existing main/auxiliary tick order.
                PrepareMotion(delta); owner.Lp.PrepareMotion(delta);
                owner.Cycle.Advance(delta, intakePressure.Value, intakeTemperature.Value,
                    throttleControl.Value, reverserControl.Value, crankRpm.Value, cylinderCockControl.Value,
                    isCylinderCrackedReadOut.Value > 0, owner.Lp.isCylinderCrackedReadOut.Value > 0, minCutoff, maxCutoff);
            }
            else if (!motionPrepared) PrepareMotion(delta);
            var state = highPressure ? owner.Cycle.Hp : owner.Cycle.Lp;
            damageThisTick = 0;
            float turns = crankRpm.Value / 60 * delta;
            crankRotationReadOut.Value = crankRotation;
            int chuffs = CheckChuff(prevCrankRotation, turns);
            motionPrepared = false;
            cylindersInletValveOpen.Value = inletMask;
            steamChestPressureReadOut.Value = (float)Math.Max(1, state.Chest.Pressure / 100000);
            intakeFlowReadOut.Value = (float)state.Intake;
            intakeFlowNormalizedReadOut.Value = (float)(state.Intake / state.MaximumIntake);
            exhaustFlowReadOut.Value = (float)state.Exhaust;
            exhaustPressureReadOut.Value = highPressure
                ? (float)Math.Max(1, owner.Cycle.Receiver.Pressure / 100000)
                : (float)Math.Max(1, state.AdmissionPressure * cutoff / 100000);
            exhaustTemperatureReadOut.Value = highPressure
                ? (float)(owner.Cycle.Receiver.Temperature - 273.15) : (float)state.ExhaustTemperature;
            float cock = Mathf.Clamp01(cylinderCockControl.Value);
            float cracked = isCylinderCrackedReadOut.Value > 0 ? 5 : 0;
            float total = cock + cracked;
            cylinderCockFlowNormalizedReadOut.Value = total > 0 ? Mathf.Clamp01((float)state.Vent * cock / total / .05f) : 0;
            cylinderCrackFlowNormalizedReadOut.Value = total > 0 ? Mathf.Clamp01((float)state.Vent * cracked / total / .05f) : 0;
            bool steamAtCylinders = state.AdmissionPressure > 100100 && inletMask != 0;
            SimulateWaterInCylinders(steamAtCylinders, chuffs, delta);
            ashesInPipesReadOut.Value = isReverserForward != (crankRpm.Value >= 0) ? cutoff : 0;
            if (chuffs > 0) damageThisTick += chuffs * ashChuffDamage * ashesInPipesReadOut.Value;
            SimulateDamage(Mathf.Abs(turns));
            float health = gameParams.DrivetrainFailuresAllowed ? NumberUtil.MapClamp(healthStateExtIn.Value, .3f, 0, 1, 0) : 1;
            torqueOut.Value = (float)state.Torque * health;
        }

        public override JObject GetSaveStateData()
        {
            var data = base.GetSaveStateData();
            data["meyerVersion"] = 1;
            data["water"] = waterInCylindersNormalizedReadOut.Value;
            data["cylinderTemperature"] = temperatureInCylinders;
            data["waterDamage"] = waterInCylindersDamageMeter;
            data["crank"] = crankRotation;
            if (hasMechanicalState)
            {
                data["mechanicsVersion"] = 1;
                var admissions = new JArray();
                for (int i = 0; i < numCylinders; i++) admissions.Add(cylinderData[i].admissionPressure);
                data["cylinderAdmissionBar"] = admissions;
            }
            if (highPressure)
            {
                var c = owner.Cycle;
                data["vessels"] = new JArray(c.Hp.Chest.Mass, c.Hp.Chest.Energy, c.Receiver.Mass, c.Receiver.Energy, c.Lp.Chest.Mass, c.Lp.Chest.Energy);
                data["ledger"] = new JArray(c.BoilerMass, c.BoilerEnergy, c.AtmosphereMass, c.AtmosphereEnergy, c.Work, c.HeatLoss);
            }
            return data;
        }

        public override void SetSaveStateData(JObject data)
        {
            if (data == null || data.Count == 0) return;
            if ((int?)data["meyerVersion"] != 1) throw new InvalidOperationException("Unsupported Meyer engine save version; refusing to replace stored steam.");
            float water = Saved(data, "water", 0, 1), temperature = Saved(data, "cylinderTemperature", 0, 2000);
            float meter = Saved(data, "waterDamage", 0, 10), crank = Saved(data, "crank", 0, 1);
            float[] admissions = SavedAdmissions(data);
            if (highPressure)
            {
                var vessels = data["vessels"] as JArray; var ledger = data["ledger"] as JArray;
                if (vessels == null || vessels.Count != 6 || ledger == null || ledger.Count != 6)
                    throw new InvalidOperationException("Incomplete Meyer compound save.");
                // Validate into detached vessels before mutating the live circuit.
                var hp = new MeyerCompoundCycle.Vessel(owner.Cycle.Hp.Chest.Volume);
                var receiver = new MeyerCompoundCycle.Vessel(owner.Cycle.Receiver.Volume);
                var lp = new MeyerCompoundCycle.Vessel(owner.Cycle.Lp.Chest.Volume);
                hp.Restore((double)vessels[0], (double)vessels[1]); receiver.Restore((double)vessels[2], (double)vessels[3]); lp.Restore((double)vessels[4], (double)vessels[5]);
                var values = new double[6];
                for (int i = 0; i < 6; i++)
                {
                    values[i] = (double)ledger[i];
                    if (!MeyerCompoundCycle.Finite(values[i]) || values[i] < 0) throw new InvalidOperationException("Invalid Meyer conservation ledger.");
                }
                double massResidual = values[0] - values[2] - hp.Mass - receiver.Mass - lp.Mass;
                double energyResidual = values[1] - values[3] - values[4] - values[5] - hp.Energy - receiver.Energy - lp.Energy;
                if (Math.Abs(massResidual) > 1e-6 || Math.Abs(energyResidual) > Math.Max(.01, values[1] * 1e-9))
                    throw new InvalidOperationException("Saved Meyer circuit violates mass/energy conservation.");
                var c = owner.Cycle;
                c.Hp.Chest.Restore(hp.Mass, hp.Energy); c.Receiver.Restore(receiver.Mass, receiver.Energy); c.Lp.Chest.Restore(lp.Mass, lp.Energy);
                c.BoilerMass = values[0]; c.BoilerEnergy = values[1]; c.AtmosphereMass = values[2]; c.AtmosphereEnergy = values[3]; c.Work = values[4]; c.HeatLoss = values[5];
            }
            waterInCylindersNormalizedReadOut.Value = water; temperatureInCylinders = temperature;
            cylinderTemperatureReadOut.Value = temperature; waterInCylindersDamageMeter = meter;
            crankRotation = prevCrankRotation = crank; crankRotationReadOut.Value = crank;
            isCylinderCrackedReadOut.Value = (bool?)data["cylCracked"] == true ? 1 : 0;
            hasMechanicalState = admissions != null;
            seedAdmissions = !hasMechanicalState;
            motionPrepared = false;
            if (admissions != null)
                for (int i = 0; i < numCylinders; i++) cylinderData[i].admissionPressure = admissions[i];
        }

        private float[] SavedAdmissions(JObject data)
        {
            var version = data["mechanicsVersion"];
            var saved = data["cylinderAdmissionBar"];
            if (version == null && saved == null) return null;
            if (version?.Type != JTokenType.Integer || (int)version != 1 ||
                !(saved is JArray array) || array.Count != numCylinders)
                throw new InvalidOperationException("Unsupported or incomplete Meyer mechanical state.");
            var values = new float[numCylinders];
            for (int i = 0; i < values.Length; i++)
            {
                var token = array[i];
                if (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
                    throw new InvalidOperationException("Invalid Meyer cylinder admission pressure.");
                double value = (double)token;
                if (!MeyerCompoundCycle.Finite(value) || Math.Abs(value) >= 1e12)
                    throw new InvalidOperationException("Invalid Meyer cylinder admission pressure.");
                values[i] = (float)value;
            }
            return values;
        }

        private static float Saved(JObject data, string name, float min, float max)
        {
            float? value = (float?)data[name];
            if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value) || value < min || value > max)
                throw new InvalidOperationException("Invalid Meyer saved " + name + ".");
            return value.Value;
        }
    }
}
