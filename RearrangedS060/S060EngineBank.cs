#if B99_S060_ARTICULATED
using System;
using System.Linq;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS060
{
    internal enum S060EngineMode { Simple = 0, Meyer = 1, Dockside = 2, Shay = 3 }

    // Two stable native port contracts. Their calibrated implementations are
    // created once with the flow. Cab, boiler, tanks and damage keep their ports.
    internal sealed class S060EngineBank
    {
        internal S060EngineMode Mode { get; private set; }
        internal int FrontConditionMode { get; private set; }
        internal S060EngineSlot Rear, Front;
        // Retain the old fields for saved-slot/test compatibility. Prefer these
        // semantic views: Rear/Front describe stable slots, not locomotive ends.
        internal S060EngineSlot Main => Rear;
        internal S060EngineSlot Auxiliary => Front;
        internal S060EngineSlot PhysicalFront => ForId(S060EngineMapping.PhysicalFrontId(Mode));
        internal S060EngineSlot PhysicalRear => ForId(S060EngineMapping.PhysicalRearId(Mode));
        internal S060EngineSlot HighPressure => Mode == S060EngineMode.Meyer ? Main : null;
        internal S060EngineSlot LowPressure => Mode == S060EngineMode.Meyer ? Auxiliary : null;
        private S060EngineSlot ForId(string id) => id == S060EngineMapping.MainId ? Main : id == S060EngineMapping.AuxiliaryId ? Auxiliary : null;
        internal MeyerCycleOwner Compound;
        internal Port ShayRpm;
        internal void Select(S060EngineMode mode)
        {
            if (!Enum.IsDefined(typeof(S060EngineMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            if (mode == Mode) return;
            var main = Main.Selected;
            var auxiliary = Auxiliary.Selected ?? Auxiliary.Engines[FrontConditionMode] ?? main;
            var mainTarget = Main.Engines[(int)mode];
            var auxiliaryTarget = Auxiliary.Engines[(int)mode];
            S060EngineState.CopyCondition(main, mainTarget, main.crankRotationReadOut.Value);
            if (mainTarget is S060ShayEngine geared) geared.SynchronizePhase();
            if (auxiliaryTarget != null) S060EngineState.CopyCondition(auxiliary, auxiliaryTarget,
                Mathf.Repeat(main.crankRotationReadOut.Value + .125f, 1));
            if (mainTarget is MeyerNativeStage hp) hp.RephaseMechanics();
            if (auxiliaryTarget is MeyerNativeStage lp) lp.RephaseMechanics();
            // A cylinder crack on either unit must survive a return to a single
            // engine, even though the auxiliary slot is then dormant.
            if (main.isCylinderCrackedReadOut.Value > 0 || auxiliary.isCylinderCrackedReadOut.Value > 0)
            {
                mainTarget.isCylinderCrackedReadOut.Value = 1;
                if (auxiliaryTarget != null) auxiliaryTarget.isCylinderCrackedReadOut.Value = 1;
            }
            Mode = mode;
            if (auxiliaryTarget != null) FrontConditionMode = (int)mode;
            Main.Publish(); Auxiliary.Publish();
        }
        internal void RestoreMode(int mode, int frontConditionMode)
        {
            if (!Enum.IsDefined(typeof(S060EngineMode), mode) || frontConditionMode < 0 || frontConditionMode > 2 || ((mode == 1 || mode == 2) && frontConditionMode != mode))
                throw new InvalidOperationException("Unsupported S060 saved engine mode.");
            Mode = (S060EngineMode)mode;
            FrontConditionMode = frontConditionMode;
        }
    }

    internal sealed class S060EngineSlotDefinition : ReciprocatingSteamEngineDefinition
    {
        internal S060EngineBank Bank;
        internal bool IsFront; // legacy name: true means the auxiliary saved slot
        internal ReciprocatingSteamEngineDefinition Simple, Dockside, Shay;
        internal MeyerStageDefinition Meyer;
        public override SimComponent InstantiateImplementation()
        {
            var slot = new S060EngineSlot(this);
            if (IsFront) Bank.Front = slot; else Bank.Rear = slot;
            return slot;
        }
    }

    // Delegates to DV's unmodified simple engine or the existing Meyer adapter.
    // The outer native engine exposes stable IDs used by the cab and audio.
    internal sealed class S060EngineSlot : ReciprocatingSteamEngine
    {
        internal readonly ReciprocatingSteamEngine[] Engines;
        internal readonly S060EngineBank Bank;
        private readonly bool auxiliary;
        private readonly Port[][] outputs;
        private readonly Port[] published;
        internal readonly Port BlastCapacity;
        internal ReciprocatingSteamEngine Selected => Engines[(int)Bank.Mode];

        internal S060EngineSlot(S060EngineSlotDefinition definition) : base(definition)
        {
            Bank = definition.Bank; auxiliary = definition.IsFront;
            Engines = new[] { auxiliary ? null : new ReciprocatingSteamEngine(definition.Simple),
                definition.Meyer.InstantiateImplementation() as ReciprocatingSteamEngine,
                new ReciprocatingSteamEngine(definition.Dockside), auxiliary || definition.Shay is null ? null : new S060ShayEngine(definition.Shay) };
            published = GetAllPorts().Where(p => p != healthStateExtIn).ToArray();
            outputs = Engines.Select(e => e == null ? null : published.Select(p => e.GetAllPorts().Single(
                q => S060EngineState.Suffix(q.id) == S060EngineState.Suffix(p.id))).ToArray()).ToArray();
            BlastCapacity = AddPort(new PortDefinition(PortType.READONLY_OUT, PortValueType.MASS_RATE, "BLAST_CAPACITY"), auxiliary ? 0 : definition.throttleMaxFlow);
            healthStateExtIn.ValueUpdatedInternally += value =>
            {
                foreach (var engine in Engines.Where(e => e != null))
                {
                    engine.healthStateExtIn.ExternalValueUpdate(healthStateExtIn.Value);
                    if (engine.isCylinderCrackedReadOut.Value > 0 && healthStateExtIn.Value > .95f && healthStateExtIn.Diff > 0)
                    { engine.isCylinderCrackedReadOut.Value = 0; engine.waterInCylindersDamageMeter = 0; }
                }
            };
        }
        public override void InitializationAfterConnecting()
        {
            foreach (var engine in Engines.Where(e => e != null))
            {
                foreach (var reference in engine.GetAllPortReferences())
                {
                    if (engine is S060ShayEngine && ReferenceEquals(reference, engine.crankRpm))
                    { reference.SetPortReference(Bank.ShayRpm ?? throw new InvalidOperationException("Shay native RPM multiplier missing.")); continue; }
                    if (auxiliary && engine is MeyerNativeStage && S060EngineState.Suffix(reference.id) == "INTAKE_PRESSURE")
                    { reference.SetPortReference(Bank.Compound.Hp.exhaustPressureReadOut); continue; }
                    if (auxiliary && engine is MeyerNativeStage && S060EngineState.Suffix(reference.id) == "INTAKE_TEMPERATURE")
                    { reference.SetPortReference(Bank.Compound.Hp.exhaustTemperatureReadOut); continue; }
                    if (auxiliary && engine is MeyerNativeStage && S060EngineState.Suffix(reference.id) == "INTAKE_QUALITY")
                    { reference.SetPortReference(new Port(id, new PortDefinition(PortType.READONLY_OUT, PortValueType.GENERIC, "RECEIVER_QUALITY"), 1)); continue; }
                    var source = GetAllPortReferences().Single(p => S060EngineState.Suffix(p.id) == S060EngineState.Suffix(reference.id));
                    if (!source.IsConnected) throw new InvalidOperationException("Unconnected S060 engine input: " + source.id);
                    reference.SetPortReference(source.port);
                }
                engine.InitializationAfterConnecting();
            }
            Publish();
        }
        public override void Tick(float delta)
        {
            var engine = Selected;
            if (engine != null)
            {
                engine.SetGameParams(gameParams);
                engine.healthStateExtIn.ExternalValueUpdate(healthStateExtIn.Value);
                engine.Tick(delta);
            }
            Publish();
        }
        internal void Publish()
        {
            var selected = Selected;
            var values = outputs[(int)Bank.Mode];
            BlastCapacity.Value = selected == null || (!auxiliary && Bank.Mode == S060EngineMode.Meyer) ? 0 : selected.throttleMaxFlow;
            for (int i = 0; i < published.Length; i++)
                published[i].Value = values == null ? 0 : values[i].Value;
            if (selected == null) return;
            numCylinders = selected.numCylinders;
            if (Bank.Mode == S060EngineMode.Shay) torqueOut.Value *= S060ShayEngine.Ratio * S060ShayEngine.Efficiency;
            if (Bank.Mode == S060EngineMode.Meyer)
            {
                if (auxiliary) { intakeFlowReadOut.Value = 0; intakeFlowNormalizedReadOut.Value = 0; }
                else exhaustFlowReadOut.Value = 0;
            }
            // Native animation consumers also read these native crank fields.
            crankRotation = selected.crankRotation;
            prevCrankRotation = selected.prevCrankRotation;
        }
        public override JObject GetSaveStateData()
        {
            var data = new JObject { ["rs060EngineVersion"] = 1 };
            if (!auxiliary) { data["mode"] = (int)Bank.Mode; data["frontConditionMode"] = Bank.FrontConditionMode; }
            for (int i = 0; i < Engines.Length; i++)
                if (Engines[i] != null) data[i.ToString()] = Engines[i] is MeyerNativeStage
                    ? Engines[i].GetSaveStateData() : Engines[i] is S060ShayEngine shay ? shay.SaveGeared() : S060EngineState.Save(Engines[i]);
            return data;
        }
        public override void SetSaveStateData(JObject data)
        {
            if (data == null || data.Count == 0) return;
            if (data["rs060EngineVersion"] == null)
            {
                if (auxiliary) throw new InvalidOperationException("Unrecognized S060 front-engine save.");
                // Legacy native save: false must not be passed to DV, whose
                // loader treats the presence of the key as a cracked cylinder.
                Engines[0].isCylinderCrackedReadOut.Value = (bool?)data["cylCracked"] == true ? 1 : 0;
                Publish(); return;
            }
            if ((int?)data["rs060EngineVersion"] != 1) throw new InvalidOperationException("Unsupported S060 engine save version.");
            // Roll back this slot and the compound circuit if any member fails.
            var before = GetSaveStateData();
            int oldMode = (int)Bank.Mode;
            int oldFront = Bank.FrontConditionMode;
            try { LoadEngines(data); if (!auxiliary) Bank.RestoreMode((int)data["mode"], (int)data["frontConditionMode"]); }
            catch { LoadEngines(before); Bank.RestoreMode(oldMode, oldFront); throw; }
            Publish();
        }
        private void LoadEngines(JObject data)
        {
            for (int i = 0; i < Engines.Length; i++)
            {
                if (Engines[i] == null) continue;
                var saved = data[i.ToString()] as JObject;
                // Pre-Shay saves contain the same original three slots. Only the
                // appended dormant circuit may be absent; old circuits stay strict.
                if (i == 3 && data["3"] == null && (int?)data["mode"] != 3) continue;
                if (saved == null) throw new InvalidOperationException("Missing S060 engine circuit state.");
                if (Engines[i] is MeyerNativeStage) Engines[i].SetSaveStateData(saved);
                else if (Engines[i] is S060ShayEngine shay) shay.LoadGeared(saved);
                else S060EngineState.Load(Engines[i], saved);
            }
        }
    }

    internal sealed class S060ShayEngine : ReciprocatingSteamEngine
    {
        internal const float Ratio = S060SteamCalibration.Shay.GearRatio, Efficiency = .92f;
        internal double GearTurns { get; private set; }
        internal S060ShayEngine(ReciprocatingSteamEngineDefinition definition) : base(definition) { }
        public override void Tick(float delta)
        {
            float previous=crankRotation;
            base.Tick(delta);
            float advanced=previous+crankRpm.Value/60f*delta;
            GearTurns = Wrap(Math.Floor(GearTurns)+Math.Floor(advanced)+crankRotation);
        }
        internal void SynchronizePhase() => GearTurns = Wrap(Math.Floor(GearTurns) + crankRotationReadOut.Value);
        private static double Wrap(double turns) => turns - Math.Floor(turns / 43) * 43;
        internal JObject SaveGeared() { var data = S060EngineState.Save(this); data["gearTurns"] = GearTurns; return data; }
        internal void LoadGeared(JObject data)
        {
            double turns = (double?)data["gearTurns"] ?? throw new InvalidOperationException("Missing Shay gear phase.");
            if (double.IsNaN(turns) || double.IsInfinity(turns) || turns < 0 || turns >= 43) throw new InvalidOperationException("Invalid Shay gear phase.");
            S060EngineState.Load(this, data);
            if (Math.Abs((turns-Math.Floor(turns))-crankRotation) > .00001)
                throw new InvalidOperationException("Shay gear phase disagrees with the saved native crank.");
            GearTurns = turns;
        }
    }
}
#endif
