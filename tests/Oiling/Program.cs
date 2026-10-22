using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using DV.Simulation.Ports;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;
using RearrangedS060;

// Only scene discovery is stubbed. Definition extension, save migration,
// dormant masks and all oil consumption/refill/damage execute production code.
namespace RearrangedS060
{
    internal static class S060Parts { internal static bool IsS060(TrainCar car) => false; }
    internal static class S060OilCups { internal static void Attach(TrainCar car) { } }
}
internal static class Program
{
    private static int checks;
    private static void Check(bool value, string text) { if (!value) throw new Exception(text); checks++; }
    private static PortDefinition P(string name) => new PortDefinition(PortType.EXTERNAL_IN, PortValueType.GENERIC, name);
    private static Port Port(string name, float value) => new Port("test", P(name), value);
    private static void Main()
    {
        var def = (ManualOilingPointsDefinition)FormatterServices.GetUninitializedObject(typeof(ManualOilingPointsDefinition));
        def.ID = "manualOiling";
        foreach (var field in typeof(ManualOilingPointsDefinition).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (field.FieldType == typeof(PortDefinition)) field.SetValue(def, P(field.Name));
            if (field.FieldType == typeof(PortReferenceDefinition)) field.SetValue(def, new PortReferenceDefinition(PortValueType.GENERIC, field.Name, field.Name == "oilConsumption"));
        }
        def.capacity = 10; def.consumptionPerRev = .01f; def.pointOpenConsumptionMultiplier = 2;
        def.leakPerHour = .36f; def.refillRate = 1; def.damagePerRevWhenEmpty = .02f;
        def.oilingPoints = Enumerable.Range(0, 6).Select(i => new ManualOilingPointsDefinition.OilingPointDefinition {
            oilLevelReadOut=P("LEVEL"+i), oilLevelNormalizedReadOut=P("NORMAL"+i), pointDoorExtIn=P("DOOR"+i), refillExtIn=P("REFILL"+i), refillingFlowNormalizedReadOut=P("FLOW"+i)
        }).ToArray();
        var originals = def.oilingPoints.ToArray(); S060Oiling.ExtendDefinition(def);
        Check(def.oilingPoints.Length == 12 && originals.Select((p,i) => ReferenceEquals(p,def.oilingPoints[i])).All(x=>x), "Original six circuits retain identities when adding big ends");
        var ids = def.oilingPoints.SelectMany(p=>new[]{p.oilLevelReadOut.ID,p.oilLevelNormalizedReadOut.ID,p.pointDoorExtIn.ID,p.refillExtIn.ID,p.refillingFlowNormalizedReadOut.ID}).ToArray();
        Check(ids.Distinct().Count()==60, "All twelve native oil points have unique ports");
        S060Oiling.ExtendDefinition(def); Check(def.oilingPoints.Length==12, "Definition extension is idempotent");
        var oil = new ManualOilingPoints(def); oil.SetGameParams(new SimGameParams(true,true,true,1,1));
        var rpm=Port("RPM",60); var storage=Port("STORAGE",100); var consumed=Port("CONSUMED",0);
        oil.wheelRpm.port=rpm; oil.oilStorage.port=storage; oil.oilConsumption.port=consumed;
        var legacy = new JArray(Enumerable.Range(0,6).Select(i=>new JObject { ["open"] = i%2==1, ["oilLevel"] = .1+i*.1 }));
        var fitted=S060Oiling.FitSaved(legacy,12);
        Check(fitted.Count==12 && legacy.Count==6 && Enumerable.Range(0,6).All(i=>JToken.DeepEquals(legacy[i],fitted[i])), "Legacy oil quantities/lids survive migration without mutation");
        oil.SetSaveStateData(new JObject { ["oilingPoints"] = fitted });
        Check(Enumerable.Range(0,6).All(i=>Math.Abs(oil.oilingPoints[i].oilLevelNormalizedReadOut.Value-(.1+i*.1))<.00001), "Actual native loader accepts migrated quantities");
        var active=Enumerable.Range(0,12).Select(i=>i<6).ToArray(); S060Oiling.ConfigureActive(oil,active);
        var dormant=oil.oilingPoints[8]; dormant.oilLevelReadOut.Value=0; dormant.oilLevelNormalizedReadOut.Value=0; dormant.pointDoorExtIn.Value=1; dormant.refillExtIn.Value=1;
        float original=oil.oilingPoints[0].oilLevelReadOut.Value;
        for(int cycle=0;cycle<100;cycle++)
        {
            S060Oiling.BeforeTick(oil); oil.Tick(.1f); S060Oiling.AfterTick(oil);
            Check(dormant.oilLevelReadOut.Value==0 && dormant.pointDoorExtIn.Value==1 && dormant.refillExtIn.Value==1, "Dormant axle oil/lid/refill state is preserved while native simulation ticks");
            Check(oil.mechanicalDamageReadOut.Value==0, "Unused empty cup does not damage active gear");
        }
        Check(oil.oilingPoints[0].oilLevelReadOut.Value < original, "Active native oil still consumes with wheel revolutions");
        active[8]=true; S060Oiling.ConfigureActive(oil,active); S060Oiling.BeforeTick(oil); oil.Tick(.1f); S060Oiling.AfterTick(oil);
        Check(oil.mechanicalDamageReadOut.Value>0, "Selected empty driven-axle cup participates in native damage");
        rpm.Value=0; S060Oiling.BeforeTick(oil); oil.Tick(1); S060Oiling.AfterTick(oil);
        Check(dormant.oilLevelReadOut.Value>0 && consumed.Value>0, "Selected native cup refills from real storage consumption port");
        var saved=oil.GetSaveStateData(); var reload=new ManualOilingPoints(def); reload.SetSaveStateData(saved);
        Check(JToken.DeepEquals(saved,reload.GetSaveStateData()), "All twelve oil quantities and lid states round trip through native save/load");
        var unused=oil.oilingPoints[10]; unused.oilLevelReadOut.Value=2.3f; unused.oilLevelNormalizedReadOut.Value=.23f; unused.pointDoorExtIn.Value=1;
        S060Oiling.BeforeTick(oil); S060Oiling.BeforeTick(oil); S060Oiling.AfterTick(oil);
        Check(unused.oilLevelReadOut.Value==oil.capacity, "Nested native callbacks retain the outer dormant mask until both callbacks finish");
        S060Oiling.AfterTick(oil);
        Check(unused.oilLevelReadOut.Value==2.3f && unused.pointDoorExtIn.Value==1, "Nested refill callbacks restore dormant state exactly once");
        oil.oilingPoints[0].oilLevelReadOut.Value=1; oil.oilingPoints[0].oilLevelNormalizedReadOut.Value=.1f;
        S060Oiling.BeforeTick(oil); oil.specialRequestExtIn.Value=1; S060Oiling.AfterTick(oil);
        Check(oil.oilingPoints[0].oilLevelReadOut.Value==oil.capacity && unused.oilLevelReadOut.Value==2.3f && unused.pointDoorExtIn.Value==1,
            "Actual native instant-service refill fills active cups while preserving dormant quantities and lids");
        TopologyTransitions(def);
        NativeCupPortRemapping(def);
        Console.WriteLine(checks+" production oil migration/mask checks passed against actual B99 oiling component; no scene interaction executed.");
    }

    private static ManualOilingPoints FreshOil(ManualOilingPointsDefinition def)
    {
        var oil = new ManualOilingPoints(def);
        oil.SetGameParams(new SimGameParams(true, true, true, 1, 1));
        oil.wheelRpm.port = Port("RPM", 60);
        oil.oilStorage.port = Port("STORAGE", 100);
        oil.oilConsumption.port = Port("CONSUMED", 0);
        foreach (var point in oil.oilingPoints)
        { point.oilLevelReadOut.Value = oil.capacity; point.oilLevelNormalizedReadOut.Value = 1; }
        return oil;
    }

    private static void Tick(ManualOilingPoints oil)
    { S060Oiling.BeforeTick(oil); oil.Tick(.1f); S060Oiling.AfterTick(oil); }

    private static void TopologyTransitions(ManualOilingPointsDefinition def)
    {
        // No external cup owner exists in this fixture: this is deliberately
        // the same call the conversion commits before delayed visual fitting.
        var oil = FreshOil(def);
        var eight = new S060Selection(S060Arrangement.EightCoupled, 1);
        S060Oiling.Select(oil, eight);
        var left = oil.oilingPoints[S060OilingTopology.ExtraIndex(true, 2)];
        var right = oil.oilingPoints[S060OilingTopology.ExtraIndex(false, 2)];
        left.oilLevelReadOut.Value = right.oilLevelReadOut.Value = 0;
        left.oilLevelNormalizedReadOut.Value = right.oilLevelNormalizedReadOut.Value = 0;
        left.pointDoorExtIn.Value = right.pointDoorExtIn.Value = 1;
        Tick(oil);
        Check(oil.mechanicalDamageReadOut.Value > 0, "Four-axle empty extra big ends contribute real native damage");

        S060Oiling.Select(oil, S060Selection.Stock);
        left.refillExtIn.Value = right.refillExtIn.Value = 1;
        left.refillingFlowNormalizedReadOut.Value = .21f;
        right.refillingFlowNormalizedReadOut.Value = .37f;
        float liveLevel = oil.oilingPoints[6].oilLevelReadOut.Value;
        for (int frame = 0; frame < 103; frame++)
        {
            Tick(oil);
            Check(oil.mechanicalDamageReadOut.Value == 0,
                "080-to-stock immediately masks obsolete empty points, including three deferred frames and permanent cup-build failure");
            Check(left.oilLevelReadOut.Value == 0 && right.oilLevelReadOut.Value == 0 &&
                left.oilLevelNormalizedReadOut.Value == 0 && right.oilLevelNormalizedReadOut.Value == 0 &&
                left.pointDoorExtIn.Value == 1 && right.pointDoorExtIn.Value == 1 &&
                left.refillExtIn.Value == 1 && right.refillExtIn.Value == 1 &&
                left.refillingFlowNormalizedReadOut.Value == .21f && right.refillingFlowNormalizedReadOut.Value == .37f,
                "Failed/absent servicing geometry cannot alter dormant quantities, lids, refill requests or refill-flow state");
        }
        Check(oil.oilingPoints[6].oilLevelReadOut.Value < liveLevel,
            "Stock extra big-end circuits consume oil before any cups are constructed");
        var retained = oil.GetSaveStateData();
        var reload = FreshOil(def); reload.SetSaveStateData(retained);
        S060Oiling.Select(reload, S060Selection.Stock); Tick(reload);
        Check(reload.mechanicalDamageReadOut.Value == 0 && reload.oilingPoints[8].oilLevelReadOut.Value == 0 &&
            reload.oilingPoints[8].pointDoorExtIn.Value == 1, "Native reload preserves dormant emptiness/lid without hidden damage");

        left.refillExtIn.Value = right.refillExtIn.Value = 0;
        S060Oiling.Select(oil, eight); Tick(oil);
        Check(oil.mechanicalDamageReadOut.Value > 0 && left.oilLevelReadOut.Value == 0 && right.oilLevelReadOut.Value == 0,
            "Reselecting 080 reactivates preserved empty points without an automatic refill");

        foreach (S060Arrangement arrangement in Enum.GetValues(typeof(S060Arrangement)))
        {
            var selection = new S060Selection(arrangement, 1);
            int expected = 4 + 2 * S060OilingTopology.DrivenAxles(arrangement);
            Check(S060OilingTopology.ActiveMask(selection).Count(x => x) == expected,
                arrangement + " topology has four original guide/crosshead circuits plus two big ends per driven axle");
            for (int empty = 0; empty < 12; empty++)
            {
                var probe = FreshOil(def);
                probe.oilingPoints[empty].oilLevelReadOut.Value = 0;
                probe.oilingPoints[empty].oilLevelNormalizedReadOut.Value = 0;
                S060Oiling.Select(probe, selection); Tick(probe);
                Check((probe.mechanicalDamageReadOut.Value > 0) == S060OilingTopology.ActiveMask(selection)[empty],
                    arrangement + " native damage agrees with synchronous selection for point " + empty);
            }
        }
        var small = new S060Selection(S060Arrangement.SixCoupled, S060Selection.SmallSixScale);
        Check(S060OilingTopology.ActiveMask(small).SequenceEqual(S060OilingTopology.ActiveMask(S060Selection.Stock)),
            "Short-wheelbase and stock 060 retain identical circuit identities");
    }

    private static void NativeCupPortRemapping(ManualOilingPointsDefinition def)
    {
        var oil = FreshOil(def);
        oil.oilingPoints[8].oilLevelReadOut.Value = 2.5f;
        oil.oilingPoints[8].pointDoorExtIn.Value = 1;
        var before = oil.GetSaveStateData();
        var originalIds = def.oilingPoints.SelectMany(p => new[] { p.oilLevelReadOut.ID,
            p.oilLevelNormalizedReadOut.ID, p.pointDoorExtIn.ID, p.refillExtIn.ID,
            p.refillingFlowNormalizedReadOut.ID }).ToArray();
        foreach (bool left in new[] { true, false })
        for (int slot = 0; slot < 3; slot++)
        {
            var from = def.oilingPoints[S060OilingTopology.NativeBigEndIndex(left)];
            var to = def.oilingPoints[S060OilingTopology.ExtraIndex(left, slot)];
            var map = S060OilCupPorts.Map(oil.id, from, to);
            // Actual B99 component types and their serialized fields; scene
            // initialization is intentionally omitted from this native fixture.
            var reader = (OilingPointPortFeederReader)FormatterServices.GetUninitializedObject(typeof(OilingPointPortFeederReader));
            reader.refillPortId = oil.id + "." + from.refillExtIn.ID;
            reader.refillingFlowNormalizedPortId = oil.id + "." + from.refillingFlowNormalizedReadOut.ID;
            var indicator = (IndicatorPortReader)FormatterServices.GetUninitializedObject(typeof(IndicatorPortReader));
            indicator.portId = oil.id + "." + from.oilLevelNormalizedReadOut.ID;
            indicator.indicatorRangeScalerPortId = "unrelated.VALUE";
            var feeder = (InteractablePortFeeder)FormatterServices.GetUninitializedObject(typeof(InteractablePortFeeder));
            feeder.portId = oil.id + "." + from.pointDoorExtIn.ID;
            var meter = (IndicatorPortReader)FormatterServices.GetUninitializedObject(typeof(IndicatorPortReader));
            meter.portId = oil.id + "." + from.oilLevelReadOut.ID;
            foreach (var component in new object[] { reader, indicator, feeder, meter }) S060OilCupPorts.Rewrite(component, map);
            Check(reader.refillPortId == oil.id + "." + to.refillExtIn.ID &&
                reader.refillingFlowNormalizedPortId == oil.id + "." + to.refillingFlowNormalizedReadOut.ID,
                "Native refill reader targets the assigned big-end circuit only");
            Check(indicator.portId == oil.id + "." + to.oilLevelNormalizedReadOut.ID &&
                meter.portId == oil.id + "." + to.oilLevelReadOut.ID &&
                indicator.indicatorRangeScalerPortId == "unrelated.VALUE" && meter.indicatorRangeScalerPortId == null,
                "Native indicators target assigned levels while unrelated and optional IDs survive");
            Check(feeder.portId == oil.id + "." + to.pointDoorExtIn.ID,
                "Native lid feeder targets its assigned saved door state");
            var values = new[] { reader.refillPortId, reader.refillingFlowNormalizedPortId,
                indicator.portId, feeder.portId, meter.portId };
            foreach (var component in new object[] { reader, indicator, feeder, meter }) S060OilCupPorts.Rewrite(component, map);
            Check(values.SequenceEqual(new[] { reader.refillPortId, reader.refillingFlowNormalizedPortId,
                indicator.portId, feeder.portId, meter.portId }), "Repeated field remapping retains the assigned IDs without native Init calls");
        }
        Check(originalIds.SequenceEqual(def.oilingPoints.SelectMany(p => new[] { p.oilLevelReadOut.ID,
            p.oilLevelNormalizedReadOut.ID, p.pointDoorExtIn.ID, p.refillExtIn.ID,
            p.refillingFlowNormalizedReadOut.ID })), "Cloned cup port maps leave native circuit definition IDs unchanged");
        Check(JToken.DeepEquals(before, oil.GetSaveStateData()), "Preparing all cloned cup port maps cannot change saved oil quantities or lid values");
    }
}
