using System;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RearrangedS060;

internal static class Program
{
    private static int checks;
    private static void Assert(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
    }
    private static void Invalid(Action action, string name)
    {
        try { action(); }
        catch (ArgumentException) { checks++; return; }
        throw new Exception("FAIL: " + name);
    }

    private static void Main()
    {
        var eight = new S060Selection(S060Arrangement.EightCoupled, .8f);
        var four = new S060Selection(S060Arrangement.FourCoupled, .9f);
        foreach (float scale in new[] { .55f, .8f, 1f, 1.15f })
            foreach (S060Arrangement id in Enum.GetValues(typeof(S060Arrangement)))
            {
                if (!S060Selection.IsSupported((int)id) || (S060Selection.RequiresFixedSize(id) && scale != 1f)) continue;
                var wanted = new S060Selection(id, scale);
                var original = new S060SavedState();
                original.BeginIdentity("car-a");
                original.RecordApplied(wanted);
                var restored = new S060SavedState();
                restored.BeginIdentity("car-a");
                Assert(restored.Load(JToken.Parse(original.Save().ToString(Formatting.None))), "payload accepted");
                Assert(restored.Pending.Value.Equals(wanted), "arrangement and size survive disk round trip");
            }
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, .54f, 1.16f })
            Invalid(() => new S060Selection(S060Arrangement.SixCoupled, bad), "invalid size rejected");
        Invalid(() => new S060Selection((S060Arrangement)99, 1f), "unknown arrangement rejected");
        Invalid(() => new S060Selection(S060Arrangement.SixTwo, .8f), "unvalidated scaled 0-6-2 rejected");
        Assert(S060Selection.Stock.IsStock, "stock includes original diameter");
        Assert(!new S060Selection(S060Arrangement.SixCoupled, .8f).IsStock, "resized six-coupled is not stock");

        var state = new S060SavedState();
        state.BeginIdentity("a");
        state.SelectInitial(eight);
        state.SelectInitial(four);
        Assert(state.Pending.Value.Equals(eight), "repeated schedule cannot overwrite pending choice");
        var savedPending = new S060SavedState();
        savedPending.Load(state.Save());
        Assert(savedPending.Pending.Value.Equals(eight), "pending intent survives save before safe application");
        state.RecordApplied(four);
        Assert(!state.Pending.HasValue && state.Applied.Equals(four), "explicit successful selection replaces pending intent");
        state.BeginIdentity("a");
        Assert(state.Applied.Equals(four), "same identity keeps choice");
        state.BeginIdentity("b");
        Assert(!state.HasSelection && !state.Pending.HasValue && state.Applied.IsStock, "pooled identity cannot inherit selection");
        state.Reset();
        Assert(state.Guid == null && !state.HasSelection, "pool reset clears identity and selection");
        Invalid(() => state.BeginIdentity(""), "empty identity rejected");
        Invalid(() => state.RecordApplied(default), "default struct cannot become saved state");

        state.BeginIdentity("old-save");
        Assert(state.Load(null), "pre-mod save accepted");
        state.SelectInitial(eight);
        Assert(state.Pending.Value.IsStock, "pre-mod saved car cannot be randomized retroactively");

        string[] malformed = {
            "{\"version\":2,\"arrangement\":2,\"driverScale\":0.8,\"future\":[1,2]}",
            "{\"version\":1,\"arrangement\":99,\"driverScale\":0.8}",
            "{\"version\":1,\"arrangement\":2,\"driverScale\":99}",
            "{\"version\":1,\"arrangement\":2}",
            "{\"version\":1,\"arrangement\":2,\"driverScale\":\"0.8\"}",
            "{\"version\":1,\"arrangement\":99999999999999999999,\"driverScale\":0.8}",
            "{\"version\":1,\"arrangement\":2,\"driverScale\":1e100}",
            "{\"version\":1,\"arrangement\":3,\"driverScale\":0.8}",
            "[1,2,3]", "false", "null"
        };
        foreach (string json in malformed)
        {
            var payload = JToken.Parse(json);
            Assert(!state.Load(payload), "unsupported or corrupt payload refused");
            state.SelectInitial(eight);
            Assert(!state.Pending.HasValue, "unknown payload does not queue a geometry edit");
            Assert(JToken.DeepEquals(payload, state.Save()), "unknown payload preserved verbatim");
            state.RecordApplied(four);
            var next = new S060SavedState();
            Assert(next.Load(state.Save()) && next.Pending.Value.Equals(four), "explicit selection replaces unknown data");
        }

        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert(state.Load(JToken.Parse("{\"version\":1,\"arrangement\":2,\"driverScale\":0.8}")), "JSON size load ignores local decimal separator");
            Assert(state.Pending.Value.Equals(eight), "locale does not change scale");
        }
        finally { CultureInfo.CurrentCulture = culture; }

        var random = new Random(42);
        Assert(S060Selection.PickInitial(false, true, true, true, .8f, random).IsStock, "randomization disabled leaves size stock");
        Assert(S060Selection.PickInitial(true, false, false, false, .8f, random).IsStock, "empty pool falls back to stock");
        for (int mask = 1; mask < 16; mask++)
        {
            var seen = new bool[4];
            for (int i = 0; i < 200; i++)
            {
                var result = S060Selection.PickInitial(true, (mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, .8f, random, (mask & 8) != 0);
                if ((mask & (1 << (int)result.Arrangement)) == 0 || result.Scale != (result.Arrangement == S060Arrangement.EightCoupled ? .8f : 1f))
                    throw new Exception("FAIL: selected disabled arrangement or wrong size");
                seen[(int)result.Arrangement] = true;
            }
            Assert(Enumerable.Range(0, 4).All(id => seen[id] == ((mask & (1 << id)) != 0)), "random pool visits enabled choices only");
        }
        state.RecordApplied(S060Selection.Stock);
#if B99_S060_ARTICULATED
        var shayState=new S060SavedState();shayState.BeginIdentity("shay");shayState.RecordApplied(new S060Selection(S060Arrangement.Shay,1));
        shayState.RecordWheelStates(new[]{0,1,2,1,0});var shaySave=shayState.Save();
        Assert((int)shaySave["version"]==4,"Only the appended Shay arrangement uses schema four");
        var shayReload=new S060SavedState();shayReload.BeginIdentity("shay");
        Assert(shayReload.Load(shaySave)&&shayReload.Pending.Value.Arrangement==S060Arrangement.Shay&&shayReload.WheelStates.SequenceEqual(new[]{0,1,2,1,0}),"Shay preserves all five dormant/active wheel condition slots");
        foreach(int old in new[]{1,2,3})
        {var invalid=(JObject)shaySave.DeepClone();invalid["version"]=old;Assert(!shayReload.Load(invalid)&&JToken.DeepEquals(invalid,shayReload.Save()),"Earlier schema cannot misinterpret the new arrangement ID");}
        var badShay=(JObject)shaySave.DeepClone();badShay["arrangement"]=0;
        Assert(!shayReload.Load(badShay)&&JToken.DeepEquals(badShay,shayReload.Save()),"Schema four with an unrelated layout is preserved as unsupported data");
#endif
        var stockReload = new S060SavedState();
        Assert(stockReload.Load(state.Save()) && stockReload.Pending.Value.IsStock, "explicit stock restoration remains stock after reload");

        var settings = new S060Settings();
        Assert(!settings.spawnRandom && settings.spawn060 && settings.spawn040 && !settings.spawn080 && !settings.spawn062 && settings.driverScale == 1f,
            "default UMM options leave randomization and experimental spawn off");
        settings.spawnRandom = true;
        settings.spawn060 = settings.spawn040 = false;
        settings.spawn080 = true;
        settings.driverScale = .8f;
        Assert(settings.PickInitial(random).Equals(eight), "actual UMM settings feed spawn selection");
        var serializer = new XmlSerializer(typeof(S060Settings));
        using (var writer = new StringWriter(CultureInfo.InvariantCulture))
        {
            serializer.Serialize(writer, settings);
            using (var reader = new StringReader(writer.ToString()))
            {
                var restored = (S060Settings)serializer.Deserialize(reader);
                Assert(restored.driverScale == .8f && restored.spawnRandom && restored.spawn080 && !restored.spawn040 && !restored.spawn060,
                    "all UMM fields survive XML serialization");
            }
        }
        using (var reader = new StringReader("<S060Settings />"))
        {
            var restored = (S060Settings)serializer.Deserialize(reader);
            Assert(restored.driverScale == 1f && !restored.spawnRandom && restored.spawn040 && !restored.spawn080,
                "missing settings fields retain safe defaults");
        }
        settings.driverScale = float.NaN;
        Assert(settings.ValidScale == 1f && settings.PickInitial(random).Scale == .8f, "legacy UMM size cannot change an authored arrangement");
        settings.spawn080 = false; settings.spawn062 = true; settings.driverScale = .8f;
        Assert(settings.PickInitial(random).Equals(new S060Selection(S060Arrangement.SixTwo, 1f)), "0-6-2 random spawn keeps validated stock drivers");
        Assert(S060TrailingLayout.PoweredAxles == 3 && S060TrailingLayout.TotalAxles == 4, "0-6-2 has one unpowered axle");
        Assert(ConventionalPhysics.FrictionForDriverWeight(.4f, S060TrailingLayout.DriverWeightFraction, 3, 4) == .4f,
            "native equal axle load leaves three quarters of weight on drivers");
        Assert(ConventionalPhysics.ReferenceOperatingMassKg == 50000f,
            "S060 adhesive design uses its 50 t working datum, not the S282's 124 t datum");
        float fourAdhesion = ConventionalPhysics.RatedDriverWeightFraction(2, 2);
        Assert(Math.Abs(fourAdhesion * ConventionalPhysics.ReferenceOperatingMassKg - 38000f) < .01f,
            "0-4-0 uses a 38 t effective adhesive allowance instead of 25 t on each driver");
        float fourFriction = ConventionalPhysics.FrictionForDriverWeight(.4f, fourAdhesion, 2, 2);
        Assert(Math.Abs(fourFriction * 50000f * 9.81f - .4f * 38000f * 9.81f) < .02f,
            "native friction conversion produces the authored 0-4-0 total grip without changing mass");
        foreach (int axles in new[] { 3, 4, 5 })
            Assert(ConventionalPhysics.RatedDriverWeightFraction(axles, axles) == 1f,
                "short six/eight/ten-coupled layouts keep full native adhesive mass without an extra-axle grip bonus");
        Assert(ConventionalPhysics.RatedDriverWeightFraction(3, 4) == S060TrailingLayout.DriverWeightFraction,
            "0-6-2 retains its explicit conservative three-quarter driver share");
        Assert(S060TrailingLayout.CentreZ - .773f / 2f > -3.470f, "nominal pony wheel clears measured coupler box band");
        Assert(-1.3978f - S060TrailingLayout.CentreZ > (1.533f + .773f) / 2f, "nominal pony wheel clears rear driver flanges");
        Assert(new S060Selection(S060Arrangement.EightCoupled, 1f).Scale == .8f, "0-8-0 uses its fixed fitted drivers");
        Assert(Math.Abs(S060Selection.StockDiameter * .8f - 1.1394048f) < .00001f, "short 0-6-0/0-8-0 have 1.14 metre tread diameter");
        var shortSix = S060Selection.ForConversion(S060Arrangement.SixCoupled, 1f);
        Assert(!shortSix.IsStock && shortSix.Scale == new S060Selection(S060Arrangement.EightCoupled, 1f).Scale && shortSix.Name == "0-6-0 Short Wheelbase", "the second 0-6-0 choice is clearly named and shares the 0-8-0 wheels");
        Assert(S060Selection.ForConversion(S060Arrangement.SixCoupled, .55f).Equals(shortSix) && S060Selection.ForConversion(S060Arrangement.FourCoupled, .55f).Scale == 1f,
            "obsolete slider settings cannot tune release arrangement sizes");
        Assert(S060Selection.Stock.Scale == 1f && S060Selection.Stock.IsStock, "stock remains a distinct full-size choice");
        foreach (float requested in new[] { .55f, .6f, .7f, .8f, 1f, 1.15f })
        {
            float fitted = S060EightLayout.FitScale(requested);
            Assert(fitted == .8f, "0-8-0 always uses its authored size, independent of legacy slider values");
            Assert(Math.Abs(S060EightLayout.Position(S060EightLayout.NativeAddedRear, fitted) - S060EightLayout.NativeRear) < .00001f,
                "fourth axle never extends behind the stock rear-driver station");
            Assert(S060EightLayout.Position(S060EightLayout.NativeGearFront, fitted) < S060EightLayout.BodyFront,
                "moving gear remains inside the measured body front");
            var cylinderOffset = S060PipeGeometry.CylinderOffset(fitted, S060EightLayout.ForwardOffset(fitted));
            Assert(3.6917117f + cylinderOffset.z < S060EightLayout.BodyFront,
                "translated full-size native cylinder/branch geometry clears the body front");
        }
        Assert(state.Load(JToken.Parse("{\"version\":1,\"arrangement\":2,\"driverScale\":1.0}")) && state.Pending.Value.Scale == .8f,
            "old full-size 0-8-0 saves migrate to the fitted layout");
#if B99_S060_ARTICULATED
        foreach (var id in new[] { S060Arrangement.Meyer, S060Arrangement.Dockside })
        {
            Invalid(() => new S060Selection(id, .8f), "articulated wheel sizes are authored independently of the slider");
            var selection = new S060Selection(id, 1f); state.RecordApplied(selection);
            Assert((int)state.Save()["version"] == 2, "articulated selections use save version 2");
            Assert(state.Load(state.Save()) && state.Pending.Value.Equals(selection), "articulated radio selection survives save/load");
            Assert(!state.Load(JObject.Parse("{\"version\":1,\"arrangement\":" + (int)id + ",\"driverScale\":1}")), "new arrangement cannot masquerade as version 1");
        }
        Assert(new S060Selection(S060Arrangement.Dockside, 1f).DriverDiameter == 1f, "radio reports Dockside's actual one-metre drivers");
        Assert(Math.Abs(new S060Selection(S060Arrangement.Shay, 1f).DriverDiameter - .8f) < .000001f,
            "Shay description reports the approved 0.80 m rolling diameter");
        Assert(S060WheelSlots.Map(new[] { -1, -1, -1, -1 }).SequenceEqual(new[] { 0, 1, 2, 3 }), "articulated axles take stable canonical wheel identities");
        Assert(S060WheelSlots.Map(new[] { 0, 1, 2, -1 }).SequenceEqual(new[] { 0, 1, 2, 3 }), "added 0-8-0 axle retains fourth damage slot");
        Assert(S060WheelSlots.Map(new[] { 0, 2 }).SequenceEqual(new[] { 0, 2 }), "0-4-0 retains the identities of its native wheels");
        Invalid(() => S060WheelSlots.Map(new[] { 0, 0 }), "duplicate native wheel identity rejected");
        var damagedWheels = new[] { 0, 1, 2, 2 };
        state.RecordApplied(S060Selection.Stock); state.RecordWheelStates(damagedWheels);
        var wheelSave = state.Save(); var reloadedWheels = new S060SavedState();
        Assert((int)wheelSave["version"] == 2 && reloadedWheels.Load(wheelSave) && reloadedWheels.WheelStates.SequenceEqual(damagedWheels), "all four wheel conditions survive a stock-layout save/reload");
        damagedWheels[3] = 0;
        Assert(reloadedWheels.WheelStates[3] == 2, "saved wheel state is detached from mutable callers");
        foreach (var bad in new[] { new JArray(0, 0, 0), new JArray(0, 0, 0, 3), new JArray(0, 0, 0, "2") })
        {
            var corrupt = (JObject)wheelSave.DeepClone(); corrupt["wheelStates"] = bad;
            Assert(!reloadedWheels.Load(corrupt) && JToken.DeepEquals(corrupt, reloadedWheels.Save()), "invalid wheel damage payload retained without partial restoration");
        }
        foreach (bool meyer in new[] { true, false })
        {
            var pick = S060Selection.PickInitial(true, false, false, false, .8f, random, false, meyer, !meyer);
            Assert(pick.Arrangement == (meyer ? S060Arrangement.Meyer : S060Arrangement.Dockside) && pick.Scale == 1f,
                "articulated random-spawn options use the chosen layout's authored size");
        }
#endif
#if B99_S060_ARTICULATED
        // New 0-10-0 IDs append; legacy v1/v2 and original oil/wheel keys remain.
        var ten = new S060Selection(S060Arrangement.TenCoupled, 1f);
        Assert((int)S060Arrangement.TenCoupled == 6 && (int)S060Arrangement.Dockside == 5, "ten-coupled ID appends without renumbering existing saves");
        Assert(ten.DriverDiameter == 1f && ten.Name.Contains("0-10-0"), "new choice reports fitted one-metre drivers");
        state.RecordApplied(ten);
        Assert((int)state.Save()["version"] == 3 && state.Load(state.Save()) && state.Pending.Value.Equals(ten), "ten-coupled selection round trips through version 3");
        var fifth = new[] { 0, 1, 2, 1, 2 };
        state.RecordWheelStates(fifth); var tenSave = state.Save();
        var tenReload = new S060SavedState();
        Assert(tenReload.Load(tenSave) && tenReload.WheelStates.SequenceEqual(fifth), "five independent wheel conditions survive save/reload");
        Assert(S060WheelSlots.Map(new[] { 0, 1, 2, -1, -1 }).SequenceEqual(new[] { 0, 1, 2, 3, 4 }), "added fourth and fifth wheels keep separate stable condition identities");
        Assert(S060Selection.PickInitial(true,false,false,false,1f,new Random(5),false,false,false,true).Equals(ten), "ten-only random pool selects fitted 0-10-0");
        Assert(Math.Abs(S060TenLayout.Position(S060TenLayout.SourceRear) - S060TenLayout.Rear) < .00001f, "fifth axle fits its authored rear station");
        Assert(S060TenLayout.Position(S060EightLayout.NativeFront) + .7582f * S060TenLayout.Scale < S060EightLayout.CouplerInnerFace - .1f, "front wheel flange clears native coupler band");
        Assert(3.6917117f + S060PipeGeometry.CylinderOffset(S060TenLayout.Scale, S060TenLayout.ForwardOffset).z < S060EightLayout.BodyFront - .05f, "full-size moved cylinders clear the body nose");
        Assert(S060TenLayout.Rear - .7582f * S060TenLayout.Scale > -2.7f, "rear flange stays forward of the cab entry station");
#endif
        Console.WriteLine(checks + " functional behavior checks passed (no Unity/game execution).");
    }
}
