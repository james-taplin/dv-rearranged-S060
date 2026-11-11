using System;

namespace RearrangedS060
{
    // A finite-volume, ideal-vapour compound model for DV. SI units throughout.
    // This is a gameplay approximation (constant gamma, no two-phase receiver),
    // not an IF97 steam plant model. Unlike two parallel native engines, LP
    // receives only conserved HP exhaust and HP works against receiver pressure.
    internal sealed class MeyerCompoundCycle
    {
        internal const double GasConstant = 461.5, Gamma = 4.0 / 3.0;
        internal const double Cv = GasConstant / (Gamma - 1), Cp = Cv + GasConstant;
        internal const double Atmosphere = 100000;

        internal struct Parcel
        {
            internal double Mass, Energy;
            internal Parcel(double mass, double energy) { Mass = mass; Energy = energy; }
        }

        internal sealed class Vessel
        {
            internal readonly double Volume;
            internal double Mass { get; private set; }
            internal double Energy { get; private set; }
            internal double Pressure => (Gamma - 1) * Energy / Volume;
            internal double Temperature => Mass > 1e-12 ? Energy / (Mass * Cv) : 298.15;
            internal Vessel(double volume) { if (!(volume > 0)) throw new ArgumentOutOfRangeException(nameof(volume)); Volume = volume; }
            internal void Add(Parcel parcel) { Mass += parcel.Mass; Energy += parcel.Energy; }
            internal Parcel Take(double requested)
            {
                double removed = Math.Min(Math.Max(0, requested), Mass);
                if (removed == 0) return default(Parcel);
                // Adiabatic blowdown of a well-mixed rigid vessel. The parcel
                // carries the exact energy lost; transferring it cannot create heat.
                double remaining = Mass - removed;
                double nextEnergy = remaining > 1e-12 ? Energy * Math.Pow(remaining / Mass, Gamma) : 0;
                var parcel = new Parcel(removed, Energy - nextEnergy);
                Mass = remaining; Energy = nextEnergy;
                return parcel;
            }
            internal double Cool(double seconds)
            {
                double heat = Math.Min(Energy, Math.Max(0, Temperature - 298.15) * 4 * seconds);
                Energy -= heat; return heat;
            }
            internal void Restore(double mass, double energy)
            {
                if (!Finite(mass) || !Finite(energy) || mass < 0 || energy < 0 || mass > 100 ||
                    energy > 1e9 || (mass == 0 && energy != 0) || (mass > 0 && energy / (mass * Cv) > 2500))
                    throw new ArgumentException("Invalid saved Meyer vapour vessel.");
                Mass = mass; Energy = energy;
            }
        }

        internal sealed class Stage
        {
            internal readonly Vessel Chest;
            internal readonly double SweptPerRevolution, MaximumIntake;
            // Native piston mechanics may request shaft torque. The circuit
            // still owns the steam transfer and caps work by actual enthalpy.
            // This delegate is runtime wiring, never part of a saved vessel.
            internal Func<double, double, double> MechanicalTorqueRequest;
            internal double Torque, Intake, Exhaust, Vent, VentEnergy, ExhaustTemperature, AdmissionPressure;
            internal Stage(double bore, double stroke, double chestCubicMetres, double maximumIntake)
            {
                if (!(bore > 0 && stroke > 0 && maximumIntake > 0)) throw new ArgumentException("Invalid Meyer stage calibration.");
                Chest = new Vessel(chestCubicMetres);
                SweptPerRevolution = 4 * Math.PI * bore * bore / 4 * stroke; // two double-acting cylinders
                MaximumIntake = maximumIntake;
            }
            internal void Clear() { Torque = Intake = Exhaust = Vent = VentEnergy = 0; }
        }

        internal readonly Stage Hp, Lp;
        internal readonly Vessel Receiver = new Vessel(.25);
        internal double BoilerMass, BoilerEnergy, AtmosphereMass, AtmosphereEnergy, Work, HeatLoss;
        internal double StoredMass => Hp.Chest.Mass + Receiver.Mass + Lp.Chest.Mass;
        internal double StoredEnergy => Hp.Chest.Energy + Receiver.Energy + Lp.Chest.Energy;
        internal double MassResidual => BoilerMass - AtmosphereMass - StoredMass;
        internal double EnergyResidual => BoilerEnergy - AtmosphereEnergy - Work - HeatLoss - StoredEnergy;

        internal MeyerCompoundCycle(double hpBore, double lpBore, double stroke, double hpChest, double lpChest, double flow)
        { Hp = new Stage(hpBore, stroke, hpChest, flow); Lp = new Stage(lpBore, stroke, lpChest, flow * 2); }

        internal void Advance(double seconds, double boilerBarAbsolute, double boilerCelsius,
            double regulator, double reverser, double rpm, double cocks, bool hpCracked, bool lpCracked,
            double minimumCutoff, double maximumCutoff)
        {
            if (!Finite(seconds) || seconds <= 0 || seconds > 1 || !Finite(boilerBarAbsolute) ||
                !Finite(boilerCelsius) || !Finite(regulator) || !Finite(reverser) || !Finite(rpm) || !Finite(cocks))
                throw new ArgumentException("Invalid Meyer compound input.");
            Hp.Clear(); Lp.Clear();
            double remaining = seconds;
            while (remaining > 1e-10)
            {
                double dt = Math.Min(.005, remaining); remaining -= dt;
                double supplyP = Math.Max(Atmosphere, boilerBarAbsolute * 100000);
                double supplyT = Math.Max(298.15, Math.Min(1500, boilerCelsius + 273.15));
                double requested = IntakeMass(supplyP, Hp.Chest.Pressure, Hp.MaximumIntake * Clamp01(regulator), dt);
                // Pressure-limited filling; source carries enthalpy, not an
                // arbitrary second boiler/pressure multiplier for the LP stage.
                double pressureRoom = Math.Max(0, supplyP * Hp.Chest.Volume / (Gamma - 1) - Hp.Chest.Energy);
                double mass = Math.Min(requested, pressureRoom / (Cp * supplyT));
                var fresh = new Parcel(mass, mass * Cp * supplyT);
                Hp.Chest.Add(fresh); Hp.Intake += mass; BoilerMass += mass; BoilerEnergy += fresh.Energy;

                double cutoff = minimumCutoff + (maximumCutoff - minimumCutoff) * Clamp01(Math.Abs(reverser));
                double sign = reverser < 0 ? -1 : 1;
                var hpExhaust = Expand(Hp, Math.Max(Atmosphere, Receiver.Pressure), cutoff, rpm, sign, dt);
                Receiver.Add(hpExhaust);
                Vent(Hp, cocks, hpCracked, dt);

                double lpMass = IntakeMass(Receiver.Pressure, Lp.Chest.Pressure, Lp.MaximumIntake, dt);
                lpMass = Math.Min(lpMass, Receiver.Mass * .1);
                double lpRoom = Math.Max(0, Receiver.Pressure * Lp.Chest.Volume / (Gamma - 1) - Lp.Chest.Energy);
                lpMass = Math.Min(lpMass, lpRoom / Math.Max(1, Cp * Receiver.Temperature));
                var transferred = Receiver.Take(lpMass);
                Lp.Chest.Add(transferred); Lp.Intake += transferred.Mass;
                var tail = Expand(Lp, Atmosphere, cutoff, rpm, sign, dt);
                AtmosphereMass += tail.Mass; AtmosphereEnergy += tail.Energy;
                Vent(Lp, cocks, lpCracked, dt);
                HeatLoss += Receiver.Cool(dt);
            }
            Hp.Torque /= seconds; Lp.Torque /= seconds;
            Hp.Intake /= seconds; Lp.Intake /= seconds;
            Hp.Exhaust /= seconds; Lp.Exhaust /= seconds;
            Hp.Vent /= seconds; Lp.Vent /= seconds;
        }

        private Parcel Expand(Stage stage, double backPressure, double cutoff, double rpm, double direction, double dt)
        {
            double pressure = stage.Chest.Pressure;
            stage.AdmissionPressure = pressure;
            if (stage.Chest.Mass <= 1e-12 || pressure <= backPressure || cutoff <= 0) return default(Parcel);
            double cut = Math.Max(.001, Math.Min(1, cutoff));
            // Integral of P dV over admission then polytropic expansion, minus
            // the actual exhaust back-pressure work over the complete stroke.
            double integral = cut + (Math.Pow(cut, Gamma) - cut) / (1 - Gamma);
            double workPerRev = Math.Max(0, (pressure * integral - backPressure) * stage.SweptPerRevolution);
            double revs = Math.Abs(rpm) / 60;
            double wanted = stage.Chest.Mass / stage.Chest.Volume * stage.SweptPerRevolution * cut * revs * dt;
            var packet = stage.Chest.Take(Math.Min(wanted, stage.Chest.Mass * .1));
            double fraction = wanted > 1e-12 ? packet.Mass / wanted : 1;
            double requestedTorque = stage.MechanicalTorqueRequest == null
                ? direction * workPerRev / (2 * Math.PI)
                : stage.MechanicalTorqueRequest(pressure, backPressure);
            if (!Finite(requestedTorque)) throw new InvalidOperationException("Invalid Meyer mechanical torque request.");
            double targetWork = Math.Abs(requestedTorque) * 2 * Math.PI * revs * dt * fraction;
            // No work may exceed the isentropic enthalpy drop of the steam
            // actually discharged. Leave the remaining energy in HP exhaust.
            double available = packet.Energy * (1 - Math.Pow(backPressure / pressure, (Gamma - 1) / Gamma));
            double work = Math.Min(targetWork, Math.Max(0, available));
            double torque = revs > 1e-9 ? work / (2 * Math.PI * revs * dt) : Math.Abs(requestedTorque);
            stage.Torque += Math.Sign(requestedTorque) * torque * dt;
            stage.Exhaust += packet.Mass;
            packet.Energy -= work; Work += work;
            stage.ExhaustTemperature = packet.Mass > 1e-12 ? packet.Energy / (packet.Mass * Cp) - 273.15 : 25;
            return packet;
        }

        private void Vent(Stage stage, double cocks, bool cracked, double dt)
        {
            double control = Clamp01(cocks) + (cracked ? 5 : 0);
            double mass = .004 * control * Math.Max(0, stage.Chest.Pressure / 100000 - 1) * dt;
            var packet = stage.Chest.Take(Math.Min(mass, stage.Chest.Mass * .1));
            stage.Vent += packet.Mass; stage.VentEnergy += packet.Energy;
            AtmosphereMass += packet.Mass; AtmosphereEnergy += packet.Energy;
        }

        private static double IntakeMass(double source, double destination, double capacity, double dt) =>
            source <= Math.Max(Atmosphere, destination) ? 0 : capacity * Math.Sqrt((source - Math.Max(Atmosphere, destination)) / source) * dt;
        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
    }
}
