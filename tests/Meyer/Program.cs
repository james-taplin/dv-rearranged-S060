using System;
using RearrangedS060;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }
    private static MeyerCompoundCycle New() => S060SteamCalibration.Meyer.CreateCycle();
    private static void Step(MeyerCompoundCycle cycle, double throttle = 1, double reverse = .6, double rpm = 80, double cocks = 0, double dt = .02)
        => cycle.Advance(dt, 16, 200, throttle, reverse, rpm, cocks, false, false, .05, .9);
    private static void Balanced(MeyerCompoundCycle c, string name)
    {
        Check(Math.Abs(c.MassResidual) < 1e-8, name + " mass conservation");
        Check(Math.Abs(c.EnergyResidual) < Math.Max(1e-5, c.BoilerEnergy * 1e-10), name + " energy conservation");
        Check(c.Hp.Chest.Mass >= 0 && c.Receiver.Mass >= 0 && c.Lp.Chest.Mass >= 0, name + " nonnegative mass");
    }
    private static int Main()
    {
        try
        {
            var empty = New();
            for (int i = 0; i < 500; i++) Step(empty, throttle: 0);
            Check(empty.BoilerMass == 0 && empty.Lp.Torque == 0 && empty.StoredMass == 0, "Unfed LP cannot manufacture steam or power");
            Balanced(empty, "empty");
            var c = New();
            for (int i = 0; i < 10000; i++) Step(c);
            Check(c.Hp.Torque > 0 && c.Lp.Torque > 0, "Both serial stages produce torque");
            Check(c.Receiver.Pressure > 1e5 && c.Receiver.Pressure < 16e5, "Receiver pressure settles between atmosphere and boiler");
            Check(c.Lp.Intake > 0 && c.Hp.Exhaust > 0 && c.Lp.Exhaust > 0, "HP exhaust feeds LP through finite receiver");
            Balanced(c, "steady");
            Console.WriteLine("steady: receiver=" + c.Receiver.Pressure / 1e5 + " bar, HP=" + c.Hp.Torque + " Nm, LP=" + c.Lp.Torque + " Nm");
            double mass = c.BoilerMass, torque = c.Lp.Torque, stored = c.StoredMass;
            for (int i = 0; i < 1000; i++) Step(c, throttle: 0);
            Check(c.BoilerMass == mass, "Closed regulator admits no new boiler steam");
            Check(c.Lp.Torque < torque * .05 && c.StoredMass < stored, "Closed regulator runs stored steam down without free sustained power");
            Balanced(c, "rundown");
            var wet = New();
            for (int i = 0; i < 2000; i++) Step(wet, cocks: 1);
            Check(wet.Hp.Vent > 0 && wet.Lp.Vent > 0, "One cab cock command vents both pressurised stage chests");
            Step(wet, cocks: 0);
            Check(wet.Hp.Vent == 0 && wet.Lp.Vent == 0, "Closing cab lever stops both uncracked cock flows");
            Balanced(wet, "cocks");
            var forward = New(); var reverse = New();
            for (int i = 0; i < 2000; i++) { Step(forward); Step(reverse, reverse: -.6, rpm: -80); }
            Check(Math.Abs(forward.Hp.Torque + reverse.Hp.Torque) < 1e-6 && Math.Abs(forward.Lp.Torque + reverse.Lp.Torque) < 1e-6, "Compound torque reverses with cab reverser");
            Check(Math.Abs(forward.BoilerMass - reverse.BoilerMass) < 1e-9, "Reverse preserves mass accounting");
            var lowBack = New(); var highBack = New();
            lowBack.Hp.Chest.Add(new MeyerCompoundCycle.Parcel(2, 16e5 * .2 / (MeyerCompoundCycle.Gamma - 1)));
            highBack.Hp.Chest.Add(new MeyerCompoundCycle.Parcel(2, 16e5 * .2 / (MeyerCompoundCycle.Gamma - 1)));
            highBack.Receiver.Add(new MeyerCompoundCycle.Parcel(2, 10e5 * .25 / (MeyerCompoundCycle.Gamma - 1)));
            Step(lowBack, throttle: 0); Step(highBack, throttle: 0);
            Check(highBack.Hp.Torque < lowBack.Hp.Torque, "Receiver back pressure reduces HP work");
            var a = New(); var b = New();
            for (int i = 0; i < 3000; i++) Step(a, dt: .02);
            for (int i = 0; i < 1200; i++) Step(b, dt: .05);
            Check(Math.Abs(a.BoilerMass - b.BoilerMass) < 1e-7 && Math.Abs(a.StoredEnergy - b.StoredEnergy) < .01, "Internal substeps give consistent results across native tick sizes");
            bool rejected = false; try { a.Receiver.Restore(-1, 1); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Corrupt negative receiver state rejected");
            MechanicalRequestChecks();
            IndependentSpeedChecks();
            Console.WriteLine(checks + " compound physics checks passed; no Unity/game execution.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void MechanicalRequestChecks()
    {
        var staticStart = New();
        staticStart.Hp.MechanicalTorqueRequest = (pressure, backPressure) => 12000;
        staticStart.Lp.MechanicalTorqueRequest = (pressure, backPressure) => 12000;
        for (int i = 0; i < 500; i++) Step(staticStart, rpm: 0);
        Check(Math.Abs(staticStart.Hp.Torque - 12000) < 1e-8 && staticStart.Lp.Torque == 0,
            "Native mechanical request produces static HP force without inventing steam for the empty LP stage");
        Check(staticStart.Work == 0 && staticStart.Hp.Exhaust == 0 && staticStart.Lp.Exhaust == 0,
            "Static requested torque produces no rotational work or swept exhaust");
        Balanced(staticStart, "native static request");
        staticStart.Hp.MechanicalTorqueRequest = (pressure, backPressure) => -12000;
        Step(staticStart, rpm: 0);
        Check(Math.Abs(staticStart.Hp.Torque + 12000) < 1e-8 && staticStart.Work == 0,
            "Native valve geometry may request reverse static torque without adding a new work ledger");

        var limited = New();
        limited.Hp.MechanicalTorqueRequest = (pressure, backPressure) => 1e12;
        limited.Lp.MechanicalTorqueRequest = (pressure, backPressure) => 1e12;
        for (int i = 0; i < 1000; i++) Step(limited);
        double requestedWork = 2e12 * 2 * Math.PI * 80 / 60 * 20;
        Check(limited.Work > 0 && limited.Work < requestedWork * .000001 && limited.Work <= limited.BoilerEnergy,
            "A large mechanical request remains bounded by real discharged-steam enthalpy, not arbitrary torque multiplication");
        Check(limited.Hp.Chest.Energy >= 0 && limited.Lp.Chest.Energy >= 0 && limited.Receiver.Energy >= 0,
            "Work capping leaves nonnegative energy in both serial stages and their receiver");
        Balanced(limited, "native enthalpy-limited request");

        var closedPhase = New();
        closedPhase.Hp.MechanicalTorqueRequest = (pressure, backPressure) => 0;
        closedPhase.Lp.MechanicalTorqueRequest = (pressure, backPressure) => 0;
        for (int i = 0; i < 1000; i++) Step(closedPhase);
        Check(closedPhase.Work == 0 && closedPhase.Hp.Torque == 0 && closedPhase.Lp.Torque == 0,
            "Closed native crank/valve phases cannot silently fall back to positive averaged torque");
        Balanced(closedPhase, "native zero-work request");
    }

    private static void IndependentSpeedChecks()
    {
        void Speeds(MeyerCompoundCycle cycle, double hp, double lp, double reverse = .6, double throttle = 1)
            => cycle.Advance(.02, 16, 200, throttle, reverse, hp, lp, 0, false, false, .05, .9);
        var legacy = New(); var equal = New();
        for (int i = 0; i < 3000; i++) { Step(legacy); Speeds(equal, 80, 80); }
        Check(legacy.StoredMass == equal.StoredMass && legacy.StoredEnergy == equal.StoredEnergy &&
            legacy.Work == equal.Work && legacy.BoilerMass == equal.BoilerMass &&
            legacy.Hp.Torque == equal.Hp.Torque && legacy.Lp.Torque == equal.Lp.Torque,
            "Equal independent speeds reproduce the original single-RPM circuit exactly");

        var emptyLp = New();
        for (int i = 0; i < 500; i++) Speeds(emptyLp, 0, 160);
        Check(emptyLp.Hp.Exhaust == 0 && emptyLp.Lp.Intake == 0 && emptyLp.Lp.Exhaust == 0 &&
            emptyLp.Lp.Torque == 0 && emptyLp.Work == 0,
            "Spinning LP cannot draw boiler steam through a stationary empty HP/receiver circuit");
        Balanced(emptyLp, "LP spinning without HP exhaust");

        var slowLp = New(); var fastLp = New();
        for (int i = 0; i < 3000; i++) { Speeds(slowLp, 80, 80); Speeds(fastLp, 80, 80); }
        for (int i = 0; i < 500; i++) { Speeds(slowLp, 80, 20); Speeds(fastLp, 80, 160); }
        Check(slowLp.Receiver.Pressure > fastLp.Receiver.Pressure && slowLp.Hp.Torque < fastLp.Hp.Torque,
            "LP speed changes finite receiver pressure and therefore real HP backpressure");
        Balanced(slowLp, "slow LP"); Balanced(fastLp, "fast LP");

        foreach (var speeds in new[] { (hp:0d, lp:80d), (hp:80d, lp:0d), (hp:20d, lp:160d),
            (hp:160d, lp:20d), (hp:-20d, lp:-160d), (hp:20d, lp:-160d), (hp:-160d, lp:20d) })
        {
            var circuit = New();
            for (int i = 0; i < 2000; i++) Speeds(circuit, speeds.hp, speeds.lp, speeds.hp < 0 ? -.6 : .6);
            Check(MeyerCompoundCycle.Finite(circuit.Hp.Torque) && MeyerCompoundCycle.Finite(circuit.Lp.Torque) &&
                circuit.Work >= 0 && circuit.Work <= circuit.BoilerEnergy,
                "Unequal/stopped/opposed stage speeds remain finite and bounded by supplied energy");
            if (speeds.lp == 0) Check(circuit.Lp.Exhaust == 0, "A stationary LP sweeps no exhaust despite rotating HP");
            double supplied = circuit.BoilerMass;
            for (int i = 0; i < 500; i++) Speeds(circuit, speeds.hp, speeds.lp, throttle: 0);
            Check(circuit.BoilerMass == supplied, "Unequal speeds do not bypass a closed boiler regulator");
            Balanced(circuit, "unequal speeds " + speeds);
        }

        double beforeMass = equal.StoredMass, beforeWork = equal.Work;
        bool rejected = false;
        try { Speeds(equal, 80, double.NaN); } catch (ArgumentException) { rejected = true; }
        Check(rejected && equal.StoredMass == beforeMass && equal.Work == beforeWork,
            "Invalid LP speed is rejected before mutating the existing circuit");
    }
}
