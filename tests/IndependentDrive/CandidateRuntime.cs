// Reflection-only adapter: all native helper code runs from the exact candidate
// DLL, loaded after the native ECS TypeManager has initialized as UMM does.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using DV.ECS.Train;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace S060SlipFixture
{
    internal static class CandidateRuntime
    {
        internal const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        internal static Assembly Assembly;
        internal static string Hash;
        internal static void Initialize()
        {
            string path = Environment.GetEnvironmentVariable("S060_SLIP_RUNTIME");
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Candidate DLL path required.");
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) Hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            Assembly = Assembly.LoadFrom(path);
        }
        internal static Type Type(string name) => Assembly.GetType("RearrangedS060." + name, true);
        internal static object InvokeType(Type type, object instance, string name, params object[] args)
        {
            try { return type.GetMethod(name, Flags).Invoke(instance, args); }
            catch (TargetInvocationException error) when (error.InnerException != null) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        internal static object Call(object instance, string name, params object[] args) => InvokeType(instance.GetType(), instance, name, args);
        internal static object Get(object instance, string name)
        {
            for (Type type = instance.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, Flags); if (field != null) return field.GetValue(instance);
                var property = type.GetProperty(name, Flags); if (property != null) return property.GetValue(instance, null);
            }
            throw new MissingFieldException(instance.GetType().Name, name);
        }
        internal static void Set(object instance, string name, object value)
        {
            for (Type type = instance.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, Flags) ?? type.GetField("<" + name + ">k__BackingField", Flags);
                if (field != null) { field.SetValue(instance, value); return; }
            }
            throw new MissingFieldException(instance.GetType().Name, name);
        }
    }
    internal static class NativeSlipEntitySchema
    {
        private static Type Type => CandidateRuntime.Type("S060NativeSlipEntitySchema");
        internal static EntityArchetype AdhesionCarrier(EntityManager manager) => (EntityArchetype)CandidateRuntime.InvokeType(Type, null, "AdhesionCarrier", manager);
        internal static EntityArchetype SlipGroup(EntityManager manager) => (EntityArchetype)CandidateRuntime.InvokeType(Type, null, "SlipGroup", manager);
        internal static void WriteGroupInputs(EntityManager manager, Entity entity, WheelslipControllerSystem.WheelslipInputData input,
            AdhesionControllerSystem.StaticSharedAdhesionCalculationData shared, AdhesionControllerSystem.AdhesionWheelslipOutputData grip,
            AdhesionControllerSystem.WheelSlideData slide) => CandidateRuntime.InvokeType(Type, null, "WriteGroupInputs", manager, entity, input, shared, grip, slide);
    }
    internal sealed class NativeSlipRuntimeState : IDisposable
    {
        private static Type Type => CandidateRuntime.Type("S060NativeSlipRuntimeState");
        internal readonly object Instance;
        internal NativeSlipRuntimeState(Transform parent, EntityManager manager, int count, int generation)
        { Instance = Activator.CreateInstance(Type, CandidateRuntime.Flags, null, new object[] { parent, manager, count, generation }, null); }
        internal static int StateCount => ((ICollection)Type.GetField("States", CandidateRuntime.Flags).GetValue(null)).Count;
        internal GameObject Root => (GameObject)CandidateRuntime.Get(Instance, "root");
        internal bool Ready => (bool)CandidateRuntime.Get(Instance, "Ready");
        internal bool HasCompletedSample => (bool)CandidateRuntime.Get(Instance, "HasCompletedSample");
        internal float BasePerAxleGrip => (float)CandidateRuntime.Get(Instance, "BasePerAxleGrip");
        private void Add(string name, Delegate value) => Type.GetEvent(name, CandidateRuntime.Flags).GetAddMethod(true).Invoke(Instance, new object[] { value });
        internal event Action PrepareInputs { add => Add("PrepareInputs", value); remove => throw new NotSupportedException(); }
        internal event Action OutputsReady { add => Add("OutputsReady", value); remove => throw new NotSupportedException(); }
        internal event Action<Exception> Failed { add => Add("Failed", value); remove => throw new NotSupportedException(); }
        internal void SetCarrier(AdhesionControllerSystem.StaticSharedAdhesionCalculationData common,
            AdhesionControllerSystem.StaticAdhesionCalculationData friction, AdhesionControllerSystem.AdhesionCalculationData calc, LocalToWorld pose)
            => CandidateRuntime.Call(Instance, "SetCarrier", common, friction, calc, pose);
        internal void SetGroup(int index, WheelslipControllerSystem.WheelslipInputData input,
            AdhesionControllerSystem.StaticSharedAdhesionCalculationData common, AdhesionControllerSystem.WheelSlideData slide, float multiplier)
            => CandidateRuntime.Call(Instance, "SetGroup", index, input, common, slide, multiplier);
        internal WheelslipControllerSystem.WheelslipOutputData GetOutput(int index) =>
            (WheelslipControllerSystem.WheelslipOutputData)CandidateRuntime.Call(Instance, "GetOutput", index);
        public void Dispose() => CandidateRuntime.Call(Instance, "Dispose");
    }
}
