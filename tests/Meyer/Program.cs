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
            Console.WriteLine(checks + " compound physics checks passed; no Unity/game execution.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
