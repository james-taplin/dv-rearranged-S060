// Narrow stand-ins for offline execution of the actual PowerDiagnostics.cs.
// These do not simulate Unity, native tick ordering, or train dynamics.
using System;
using System.Collections.Generic;

namespace HarmonyLib {
    [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch : Attribute {
        public HarmonyPatch(Type type, string method) { }
    }
}
namespace UnityEngine {
    public static class Time {
        public static float realtimeSinceStartup, time, deltaTime = .02f;
        public static int frameCount;
    }
    public static class Mathf {
        public const float PI = (float)Math.PI;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Math.Max(0f, Math.Min(1f, t));
    }
    public struct Vector3 { public float y; }
    public sealed class Transform { public Vector3 forward; }
}
namespace DV { public static class TimeUtil { public static bool IsFlowing = true; } }
namespace DV.Utils { }
namespace DV.ThingTypes { public enum TrainCarType { LocoSteamHeavy, Other } }
namespace LocoSim.Implementations {
    public class Port { public float Value; public string id; }
    public sealed class PortReference { public float Value; }
    public sealed class ReciprocatingSteamEngine {
        public readonly PortReference crankRpm = new(), throttleControl = new(), reverserControl = new(),
            intakePressure = new(), cylinderCockControl = new();
        public Port torqueOut = new(), steamChestPressureReadOut = new(), intakeFlowReadOut = new(), exhaustFlowReadOut = new();
        public Port crankRotationReadOut = new(), chuffEventReadOut = new(), chuffFrequencyReadOut = new();
        public int numCylinders = 2;
        public float cutoff, admissionPressure, steamChestSteamMass;
    }
    public sealed class SimulationFlow {
        public readonly List<object> OrderedSimComps = new();
        public readonly Dictionary<string, Port> Ports = new();
        public bool TryGetPort(string id, out Port port) => Ports.TryGetValue(id, out port);
    }
}
namespace DV.Wheels {
    public sealed class WheelslipController {
        public float wheelslip, OrientedMaxWheelslipRpm, maxWheelslipRpm = 600;
        public bool IsWheelslipping => wheelslip > 0;
    }
    public sealed class AdhesionController {
        public float wheelSlide;
        public bool IsWheelSliding => wheelSlide > 0;
    }
}
namespace DV.Simulation.Ports {
    public sealed class TractionPortsFeeder { public string wheelRpmPortId = "traction.WHEEL_RPM_EXT_IN"; public float wheelRpm; }
}
namespace DV.Simulation.Cars {
    public sealed class DrivingForce { public float wheelRadius = .712f, generatedForce; }
    public sealed class SimController {
        public TrainCar train;
        public LocoSim.Implementations.SimulationFlow simFlow = new();
        public DV.Simulation.Ports.TractionPortsFeeder tractionPortsFeeder = new();
        public DrivingForce drivingForce = new();
        public DV.Wheels.WheelslipController wheelslipController = new();
        public float simTimeMultiplier = 1;
    }
}
public sealed class TrainCar {
    public string ID = "L-001";
    public DV.ThingTypes.TrainCarType carType = DV.ThingTypes.TrainCarType.LocoSteamHeavy;
    public bool derailed;
    public float Speed;
    public DV.Wheels.AdhesionController adhesionController = new();
    public readonly UnityEngine.Transform transform = new();
    public RearrangedS282.WheelRearranger Rearranger;
    public float GetForwardSpeed() => Speed;
    public T GetComponent<T>() where T : class => Rearranger as T;
}
namespace UnityModManagerNet {
    public static class UnityModManager {
        public static readonly List<ModEntry> modEntries = new();
        public sealed class ModEntry { public bool Active; public ModInfo Info = new(); }
        public sealed class ModInfo { public string Id, Version; }
    }
}
namespace RearrangedS282 {
    public sealed class WheelRearranger { public WheelArrangementType currentWA; }
    internal static class Stage2RuntimeChecks { internal static bool Running; }
    public static class Main {
        public static string ModPath;
        public static readonly TestLogger Logger = new();
    }
    public sealed class TestLogger {
        public readonly List<string> Messages = new();
        public bool ThrowOnLogException;
        public void Log(string message) => Messages.Add(message);
        public void LogException(string message, Exception exception) {
            if (ThrowOnLogException) throw new System.IO.IOException("Diagnostic logger unavailable");
            Messages.Add(message + ": " + exception.Message);
        }
    }
}
