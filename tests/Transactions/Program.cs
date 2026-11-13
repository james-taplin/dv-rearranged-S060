using System;
using System.Collections.Generic;
using RearrangedS060;

// The real conversion registry, SwitchTo, rollback and pool ownership code is
// linked above. Scene discovery/rendering/native simulation are deliberate test
// doubles; these tests establish owner boundaries, not Unity physics behavior.
static class Program
{
    static int checks, scenarios;
    static readonly S060Selection Four = new(S060Arrangement.FourCoupled, 1), Eight = new(S060Arrangement.EightCoupled, 1);
    static void Check(bool ok, string why) { checks++; if (!ok) throw new Exception(why); }
    static Exception Fail(Action action)
    { try { action(); } catch (Exception ex) { return ex; } throw new Exception("Expected injected failure"); }
    static TrainCar Car() { Faults.Reset(); scenarios++; return new TrainCar(); }
    static void Clean(TrainCar car) { Faults.Reset(); S060Conversion.Release(car); Check(!Faults.Resources.Contains(car), "Owned resource released"); }
    static void Main()
    {
        var unsettled = Car();
        unsettled.Independent = new S060IndependentDrive { Settled = false };
        Check(Fail(() => S060Conversion.SwitchTo(unsettled, Four)).Message.Contains("both powered sets"),
            "Independent wheel motion blocks switching even with pooled slip/force at zero");
        Check(Faults.ApplyCalls == 0 && S060Conversion.Current(unsettled) == null,
            "Unsettled independent drive cannot mutate the arrangement");
        unsettled.Independent.Settled = true;
        S060Conversion.SwitchTo(unsettled, Four);
        Check(S060Conversion.Current(unsettled)?.Selection.Equals(Four) == true,
            "Settled independent drive permits the existing conversion path");
        Clean(unsettled);

        var car = Car(); S060Conversion.SwitchTo(car, Four);
        Check(S060Conversion.Current(car).Selection.Equals(Four), "Successful apply owns correct selection");
        S060Conversion.Restore(car); Check(S060Conversion.Current(car) == null && !Faults.Resources.Contains(car), "Successful restore releases owner/resources");

        car = Car(); Faults.ApplyFailures = 1;
        Check(Fail(() => S060Conversion.SwitchTo(car, Four)).Message == "injected apply", "Original apply exception preserved after successful rollback");
        Check(S060Conversion.Current(car) == null && !Faults.Resources.Contains(car), "Successful rollback leaves stock and no temporary owner");

        car = Car(); Faults.ApplyFailures = Faults.UndoFailures = 1;
        var failure = Fail(() => S060Conversion.Convert<S060EightCoupled>(car, 1));
        var retained = S060Conversion.Current(car);
        Check(failure is AggregateException && retained?.CleanupPending == true, "Failed rollback retains owner and both errors");
        Check(retained.Selection.Equals(Eight), "Requested selection remains stable after scale restoration");
        Check(Faults.Resources.Contains(car) && Faults.ScaleRestores == 1 && Faults.AudioRefreshes == 1, "Undo failure cannot skip independent scale/axle stages");
        S060Conversion.SwitchTo(car, Eight);
        Check(S060Conversion.Current(car)?.CleanupPending == false && Faults.ApplyCalls == 2, "Same requested selection retries cleanup before equality return"); Clean(car);

        car = Car(); S060Conversion.SwitchTo(car, Four); Faults.PreflightFailures = 1;
        retained = S060Conversion.Current(car);
        Fail(() => S060Conversion.SwitchTo(car, new S060Selection(S060Arrangement.Meyer, 1)));
        Check(ReferenceEquals(retained, S060Conversion.Current(car)) && Faults.UndoCalls == 0 && Faults.ApplyCalls == 1,
            "Missing/invalid donor preflight leaves prior owner and geometry intact"); Clean(car);

        car = Car(); S060Conversion.SwitchTo(car, Four); Faults.PreflightFailures = 1;
        retained = S060Conversion.Current(car);
        Fail(() => S060Conversion.SwitchTo(car, new S060Selection(S060Arrangement.SixTwo, 1)));
        Check(ReferenceEquals(retained, S060Conversion.Current(car)) && Faults.UndoCalls == 0,
            "Missing native pony preflight also preserves prior layout"); Clean(car);

        car = Car(); S060Conversion.SwitchTo(car, Four); Faults.ApplyFailures = 1;
        Fail(() => S060Conversion.SwitchTo(car, Eight));
        Check(S060Conversion.Current(car).Selection.Equals(Four) && !S060Conversion.Current(car).CleanupPending, "Clean failed replacement recovers prior selection");
        Check(Faults.ApplyCalls == 3 && Faults.Resources.Contains(car), "Only successful rollback allows previous arrangement to be reapplied"); Clean(car);

        car = Car(); S060Conversion.SwitchTo(car, Four); Faults.ApplyFailures = 1; Faults.FailUndoAfter = 2;
        Fail(() => S060Conversion.SwitchTo(car, Eight));
        Check(S060Conversion.Current(car).CleanupPending && Faults.ApplyCalls == 2, "Unclean replacement does not stack previous arrangement over remaining resources");
        Check(S060Conversion.Current(car).Selection.Equals(Eight), "Failed target cleanup remains the registered owner");
        S060Conversion.SwitchTo(car, S060Selection.Stock);
        Check(S060Conversion.Current(car) == null && !Faults.Resources.Contains(car), "Explicit stock retries pending cleanup");

        car = Car(); S060Conversion.SwitchTo(car, Four); Faults.UndoFailures = 1;
        Fail(() => S060Conversion.SwitchTo(car, Eight));
        Check(S060Conversion.Current(car).CleanupPending && Faults.ApplyCalls == 1, "Failed old-layout restoration prevents target Apply");
        S060Conversion.SwitchTo(car, Four); Check(Faults.ApplyCalls == 2 && !S060Conversion.Current(car).CleanupPending, "Failed old owner can retry and rebuild safely"); Clean(car);

        car = Car(); S060Conversion.SwitchTo(car, Four); Faults.RodDisposeFailures = 1;
        Fail(() => S060Conversion.Restore(car));
        Check(Faults.UndoCalls == 1 && Faults.ScaleRestores == 1 && Faults.AudioRefreshes == 2,
            "Rod-disposal failure still attempts Undo, scale and axle refresh");
        Check(S060Conversion.Current(car).CleanupPending, "Failed rod owner retained for retry");
        S060Conversion.Restore(car); Check(S060Conversion.Current(car) == null && Faults.RodDisposals == 2, "Rod disposal retried without losing its owner");

        car = Car(); S060Conversion.SwitchTo(car, Four); Faults.ScaleRestoreFailures = 1;
        Fail(() => S060Conversion.Restore(car));
        Check(Faults.UndoCalls == 1 && Faults.AudioRefreshes == 2 && S060Conversion.Current(car).CleanupPending,
            "Scale failure still attempts axle/audio restoration and retains owner"); Clean(car);

        car = Car(); S060Conversion.SwitchTo(car, Four); Faults.CupReleaseFailures = 1;
        Fail(() => S060Conversion.Release(car));
        Check(Faults.UndoCalls == 1 && Faults.ScaleRestores == 1 && S060Conversion.Current(car).CleanupPending,
            "Cup teardown failure cannot skip running-gear pool cleanup");
        Clean(car); S060Conversion.Release(car); Check(S060Conversion.Current(car) == null, "Repeated successful pool release is idempotent");

        car = Car(); Faults.CupRefreshFailures = 1;
        Fail(() => S060Conversion.SwitchTo(car, Four));
        Check(S060Conversion.Current(car) == null && !Faults.Resources.Contains(car), "Post-apply servicing failure also rolls back owned geometry");

        car = Car(); S060Conversion.SwitchTo(car, Four); Faults.OilSelectFailures = 1;
        Fail(() => S060Conversion.Restore(car));
        Check(S060Conversion.Current(car).CleanupPending, "Post-geometry oil selection failure retains a retryable owner"); Clean(car);

        car = Car(); Faults.ApplyFailures = Faults.UndoFailures = 1;
        Fail(() => S060Conversion.Convert<S060FourCoupled>(car, 1));
        S060Conversion.ForgetDestroyed(car); S060Conversion.ForgetDestroyed(car);
        Check(S060Conversion.Current(car) == null && Faults.MeshReleases == 1, "Destruction fallback releases and forgets pending owner exactly once");
        Check(car.Guid == "native-guid" && car.Fuel == 123 && car.Water == 456 && car.Damage == .2f, "Registry and cleanup operations preserve native identity/resources");
        Console.WriteLine($"{checks} production conversion-boundary checks across {scenarios} injected scenarios passed; scene/native simulation discovery are test doubles.");
    }
}

static class Faults
{
    internal static int ApplyFailures, UndoFailures, PreflightFailures, RodDisposeFailures, ScaleRestoreFailures, CupReleaseFailures, CupRefreshFailures, OilSelectFailures, FailUndoAfter;
    internal static int ApplyCalls, UndoCalls, ScaleRestores, AudioRefreshes, RodDisposals, MeshReleases;
    internal static readonly HashSet<TrainCar> Resources = new();
    internal static void Reset()
    { ApplyFailures = UndoFailures = PreflightFailures = RodDisposeFailures = ScaleRestoreFailures = CupReleaseFailures = CupRefreshFailures = OilSelectFailures = FailUndoAfter = 0;
      ApplyCalls = UndoCalls = ScaleRestores = AudioRefreshes = RodDisposals = MeshReleases = 0; }
    internal static void Throw(ref int remaining, string message) { if (remaining > 0) { remaining--; throw new Exception(message); } }
}

public class TrainCar
{
    public string ID = "test", Guid = "native-guid";
    public float Fuel = 123, Water = 456, Damage = .2f;
    public bool derailed, isExploded;
    internal S060IndependentDrive Independent;
    public UnityEngine.Transform transform = new();
    public Sim SimController = new(); public Adhesion adhesionController = new();
    public float GetForwardSpeed() => 0;
    public T GetComponent<T>() where T : class => null;
    public class Sim { public Flow simFlow = new(); public Force drivingForce = new(); public Slip wheelslipController = new(); }
    public class Flow { public bool TryGetPort(string _, out Port port) { port = new(); return true; } }
    public class Port { public float Value; }
    public class Force { public float generatedForce; }
    public class Slip { public bool IsWheelslipping; }
    public class Adhesion { public bool IsWheelSliding; }
}

namespace UnityEngine
{
    public class MonoBehaviour { public Transform transform = new(); }
    public class Transform
    {
        public Vector3 localPosition; public Quaternion localRotation; public Transform parent => this;
        public Vector3 position => localPosition;
        public Vector3 InverseTransformPoint(Vector3 p) => p;
        public Vector3 TransformPoint(Vector3 p) => p;
    }
    public struct Vector3 { public float x, y, z; public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; } public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x+b.x,a.y+b.y,a.z+b.z); }
    public struct Quaternion { }
    public static class Mathf { public static float Abs(float n) => Math.Abs(n); }
}

namespace RearrangedS060
{
    using UnityEngine;
    // The real native traction owner is exercised by tests/IndependentDrive.
    // This boundary double supplies only the switch-readiness contract.
    internal sealed class S060IndependentDrive
    {
        internal bool Settled = true;
        internal static S060IndependentDrive For(TrainCar car) => car.Independent;
    }
    internal static class Main { internal static LoggerStub Logger = new(); internal class LoggerStub { public void LogException(string _, Exception ex) {} public void Log(string _) {} } }
    internal class S060Parts
    {
        internal TrainCar Car; internal Bogie FrontBogie = new(), RearBogie = new();
        internal static bool IsS060(TrainCar c) => c != null;
        internal static S060Parts Resolve(TrainCar c) => new() { Car = c };
        internal class Bogie { internal object axles; }
    }
    internal class B99PhysicsState { internal B99PhysicsState(TrainCar _, int __) {} }
    internal class S060DriverScale
    {
        internal float Factor = 1; internal S060DriverScale(S060Parts _) {}
        internal void Apply(float scale) => Factor = scale;
        internal void PositionFittings(float shift) {}
        internal void RetireFixedValveGear() {} // Native geometry is covered by ConventionalNativeAudit.
        internal void Restore() { Faults.ScaleRestores++; Factor = 1; Faults.Throw(ref Faults.ScaleRestoreFailures, "injected scale restore"); }
        internal void ReleaseResources() => Faults.MeshReleases++;
    }
    internal class S060CouplingRods : IDisposable
    {
        internal static S060CouplingRods Apply(S060Parts _) => new();
        public void Dispose() { Faults.RodDisposals++; Faults.Throw(ref Faults.RodDisposeFailures, "injected rod disposal"); }
    }
    internal static class S060RollingAudio { internal static void Refresh(TrainCar _) => Faults.AudioRefreshes++; }
    internal static class S060Oiling { internal static void Select(TrainCar _, S060Selection __) => Faults.Throw(ref Faults.OilSelectFailures, "injected oil select"); }
    internal static class S060OilCups
    {
        internal static void Refresh(TrainCar _) => Faults.Throw(ref Faults.CupRefreshFailures, "injected cup refresh");
        internal static void Release(TrainCar _) => Faults.Throw(ref Faults.CupReleaseFailures, "injected cup release");
    }
    internal class S060MeyerSupport { internal bool Active = false; internal static void RequireRearFree(TrainCar _) {} internal void Restore() {} }
    internal enum S060EngineMode { Simple }
    internal class S060ArticulatedSimulation { internal TrainCar.Flow Flow = null; internal static void Ensure(TrainCar _) {} internal void Select(S060EngineMode _) {} }
    internal static class S060Articulated { internal static void Preflight(bool _) => Faults.Throw(ref Faults.PreflightFailures, "injected donor preflight"); }
    internal static class S060PonyAssets { internal static void Resolve() => Faults.Throw(ref Faults.PreflightFailures, "injected pony preflight"); }
    internal class ProbeConversion : S060Conversion
    {
        internal override string Name => "test conversion";
        protected override void Apply() { Faults.ApplyCalls++; if (!Faults.Resources.Add(Parts.Car)) throw new Exception("Overlapping geometry ownership"); Faults.Throw(ref Faults.ApplyFailures, "injected apply"); }
        protected override void Undo()
        { Faults.UndoCalls++; if (Faults.FailUndoAfter == Faults.UndoCalls) throw new Exception("injected rollback");
          Faults.Throw(ref Faults.UndoFailures, "injected undo"); Faults.Resources.Remove(Parts.Car); }
    }
    internal class S060TenCoupled : ProbeConversion {}
    internal class S060FourCoupled : ProbeConversion {} internal class S060EightCoupled : ProbeConversion {}
    internal class S060SixTwo : ProbeConversion {} internal class S060Rescaled : ProbeConversion {}
    internal class S060Meyer : ProbeConversion {} internal class S060Dockside : ProbeConversion {}
    internal class S060Shay : ProbeConversion {}
    internal static class S060ShayAssets { internal static void Resolve() {} }
}
