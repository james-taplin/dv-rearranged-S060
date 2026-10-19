$ErrorActionPreference = 'Stop'
$output = Join-Path $PSScriptRoot 'build\random-lifecycle-validation'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$harness = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json.Linq;
namespace UnityEngine {
 [AttributeUsage(AttributeTargets.Field,AllowMultiple=true)] public class HeaderAttribute:Attribute { public HeaderAttribute(string s){} }
 [AttributeUsage(AttributeTargets.Field)] public class SpaceAttribute:Attribute { public SpaceAttribute(int n){} }
}
namespace UnityModManagerNet {
 public enum DrawType { Auto, Toggle, ToggleGroup, Slider }
 [AttributeUsage(AttributeTargets.Field)] public class DrawAttribute:Attribute { public int Height,Precision; public double Min,Max; public string Label; public DrawType Type; public DrawAttribute(string s,DrawType t=DrawType.Auto){Label=s;Type=t;} }
 public interface IDrawable {}
 public class UnityModManager { public class ModEntry {} public class ModSettings { public virtual void Save(ModEntry entry){} public static void Save<T>(T value,ModEntry entry){} } }
}
namespace RearrangedS282 {
 public static class RandomLifecycleHarness {
  static int checks;
  static void Check(bool pass,string message){if(!pass)throw new Exception(message);checks++;}
  public static int Run(){
   var random=new Random(23135);
   var settings=new Settings();settings.OnChange();
   Check(!settings.spawnRandomWA,"Random spawning must be opt-in");
   var highSpeed=new[]{12,13,14};
   Check(settings.RandomWAs.Count==30 && settings.RandomWAs.Distinct().Count()==30,"Default pool must contain the30 useful conventional choices exactly once");
   Check(settings.RandomWAs.Select(a=>(int)a).OrderBy(a=>a).SequenceEqual(Enumerable.Range(0,31).Where(i=>!highSpeed.Contains(i)).Concat(new[]{34,35})),"Default pool: old31 minus the high-speed variants, plus Mogul/2-4-0");
   var old=new Settings{spawnPoolRevision=0,spawn280Big=true,spawn282Big=true,spawn284Big=true,spawn440=false};
   Check(old.MigrateSpawnPool() && !old.spawn280Big && !old.spawn282Big && !old.spawn284Big && !old.spawn440 && old.spawnPoolRevision==Settings.CurrentSpawnPoolRevision,"Older settings lose only the high-speed variants from the pool");
   old.spawn282Big=true;Check(!old.MigrateSpawnPool() && old.spawn282Big,"A player's later choice survives the one-time migration");
   Check(settings.RandomWAs.All(a=>WheelArrangement.IsConventionalId((int)a)),"Pool includes unsupported ID");
   Check(!settings.spawn222 && !settings.RandomWAs.Contains(WheelArrangementType.s222),"Experimental 2-2-2 must remain opt-in");
   Check(settings.giant222DriverDiameterMetres==2.8f,"Experimental driver DIAMETER default must be2.8m");
   var diameterDraw=(UnityModManagerNet.DrawAttribute)Attribute.GetCustomAttribute(typeof(Settings).GetField("giant222DriverDiameterMetres"),typeof(UnityModManagerNet.DrawAttribute));
   Check(diameterDraw.Type==UnityModManagerNet.DrawType.Slider && diameterDraw.Min==2.1 && diameterDraw.Max==3.6 && diameterDraw.Precision==2,"Existing UMM diameter slider must declare2.1–3.6m range");
   Check(settings.polishedPacificHudsonValveGear && !settings.show6CoupledValveGear,"Pacific/Hudson polish default must not change saved global six-coupled default");
   var persistedSettings=new Settings{giant222DriverDiameterMetres=3.4f,polishedPacificHudsonValveGear=false,spawn222=true};
   var serializer=new XmlSerializer(typeof(Settings));var writer=new StringWriter();serializer.Serialize(writer,persistedSettings);
   var restoredSettings=(Settings)serializer.Deserialize(new StringReader(writer.ToString()));
   Check(restoredSettings.giant222DriverDiameterMetres==3.4f && !restoredSettings.polishedPacificHudsonValveGear && restoredSettings.spawn222,"Diameter, polish preference and experimental opt-in must serialize through existing settings format");
   var oldNames="s440,s442,s444,s460,s462,s464,s080,s082,s084,s280,s282,s284,s280Big,s282Big,s284Big,s480,s482,s484,s0100,s0102,s0104,s2100,s2102,s2104,s4100,s4102,s4104,s0120,s2120,s2122,s4122,s2442,s4444,s404".Split(',');
   for(int id=0;id<oldNames.Length;id++)Check((int)Enum.Parse(typeof(WheelArrangementType),oldNames[id])==id,"Legacy save ID moved: "+oldNames[id]);
   Check((int)WheelArrangementType.s260==34 && (int)WheelArrangementType.s240==35 && (int)WheelArrangementType.s222==36,"New conventional IDs must append at34/35/36");
   Check((int)WheelArrangementType.s2662==37 && (int)WheelArrangementType.s22222==38 && (int)WheelArrangementType.s4144==39 && (int)WheelArrangementType.s02220==40 && (int)WheelArrangementType.s08880==41 && (int)WheelArrangementType.s2680==42 && (int)WheelArrangementType.s28882==43,"Mallet, triplex, 4-14-4, 0-2-2-2-0, 0-8-8-0, 2-6-8-0 and 2-8-8-8-2 must append at37..43");
   Check(WheelArrangement.SupportedCount==44 && WheelArrangement.WheelArrangementNames.Length==44 && WheelArrangement.NumOfNondrivenWheels.Length==44,"Catalogue metadata must contain all44 IDs");
   Check(WheelArrangement.WheelArrangementNames[43]=="2-8-8-8-2 Erie Triplex" && WheelArrangement.IsArticulatedId(43) && string.Join("-",WheelArrangement.LocomotiveWheels(43))=="2-8-8-0" && string.Join("-",WheelArrangement.LocomotiveWheels(10))=="2-8-2","Erie triplex keeps its tender engine off the locomotive axle count");
   Check(WheelArrangement.WheelArrangementNames[41]=="0-8-8-0 Angus" && WheelArrangement.NumOfNondrivenWheels[41]==0 && WheelArrangement.IsArticulatedId(41) && WheelArrangement.WheelArrangementNames[42]=="2-6-8-0 Gt. Northern" && WheelArrangement.NumOfNondrivenWheels[42]==2 && WheelArrangement.IsArticulatedId(42),"0-8-8-0 and 2-6-8-0 are parseable articulated entries");
   Check(WheelArrangement.WheelArrangementNames[40]=="0-2-2-2-0 Articulated Triplex" && WheelArrangement.NumOfNondrivenWheels[40]==0 && WheelArrangement.IsArticulatedId(40) && !WheelArrangement.IsConventionalId(40),"0-2-2-2-0 is a parseable articulated entry with no carrying wheels");
   Check(WheelArrangement.WheelArrangementNames[39]=="4-14-4" && WheelArrangement.NumOfNondrivenWheels[39]==8 && WheelArrangement.IsConventionalId(39) && !WheelArrangement.IsArticulatedId(39),"4-14-4 is a parseable conventional entry with8 non-driven wheels");
   Check(WheelArrangement.WheelArrangementNames[38]=="2-2-2-2-2 Articulated Triplex" && WheelArrangement.NumOfNondrivenWheels[38]==4,"Triplex name must keep a parseable Whyte prefix and4 non-driven wheels");
   Check(WheelArrangement.WheelArrangementNames[37]=="2-6-6-2 Articulated Mallet" && WheelArrangement.NumOfNondrivenWheels[37]==4,"Mallet name must keep a parseable Whyte prefix and4 non-driven wheels");
   Check(WheelArrangement.WheelArrangementNames[34]=="2-6-0 Mogul" && WheelArrangement.WheelArrangementNames[35]=="2-4-0" && WheelArrangement.WheelArrangementNames[36]=="2-2-2 Giant-driver clearance prototype","New names must retain parseable Whyte prefixes");
   Check(WheelArrangement.NumOfNondrivenWheels[34]==2 && WheelArrangement.NumOfNondrivenWheels[35]==2 && WheelArrangement.NumOfNondrivenWheels[36]==4,"New non-driven wheel counts are2/2/4");
   for(int id=0;id<44;id++)Check(WheelArrangement.IsStandardId(id)==(id<=30),"Historical IsStandardId semantics changed");
   Check(WheelArrangement.IsConventionalId(34) && WheelArrangement.IsConventionalId(35) && WheelArrangement.IsConventionalId(36) && !WheelArrangement.IsConventionalId(31) && !WheelArrangement.IsConventionalId(32) && !WheelArrangement.IsConventionalId(33) && !WheelArrangement.IsConventionalId(37) && !WheelArrangement.IsConventionalId(38),"New conventional helper must keep articulated/novelty classification separate");
   Check(WheelArrangement.IsArticulatedId(31) && WheelArrangement.IsArticulatedId(32) && WheelArrangement.IsArticulatedId(37) && WheelArrangement.IsArticulatedId(38) && !WheelArrangement.IsArticulatedId(36),"Mallet and triplex must be classified articulated");
   var fields=typeof(Settings).GetFields().Where(f=>f.FieldType==typeof(bool) && f.Name.StartsWith("spawn") && f.Name!="spawnRandomWA").ToArray();
   Check(fields.Length==44,"Every supported arrangement needs one pool toggle");
   Check(!settings.spawn2442 && !settings.spawn4444 && !settings.spawn2662 && !settings.spawn22222 && !settings.spawn02220 && !settings.spawn08880 && !settings.spawn2680 && !settings.spawn28882 && !settings.spawn4144 && !settings.spawn404,"Articulated and novelty random choices must be opt-in");
   for(int id=0;id<WheelArrangement.SupportedCount;id++){
    Check(WheelArrangement.IsRuntimeId(id),"Radio catalogue excludes supported arrangement");
    int next=WheelArrangement.NextRuntimeId(id,1);
    Check(WheelArrangement.NextRuntimeId(next,-1)==id,"Radio forward/reverse traversal lost an ID");
   }
   Check(WheelArrangement.IsRuntimeId(43) && !WheelArrangement.IsRuntimeId(44),"New ID43 selectable; unknown ID44 rejected");
   foreach(var field in fields){
    foreach(var other in fields)other.SetValue(settings,false);
    field.SetValue(settings,true);settings.OnChange();
    var name=field.Name.Substring(5);var expected=(WheelArrangementType)Enum.Parse(typeof(WheelArrangementType),"s"+name);
    Check(settings.RandomWAs.Count==1 && settings.RandomWAs[0]==expected,"Wrong toggle mapping: "+field.Name);
    Check(settings.PickRandomArrangement(random)==(int)expected,"Single-choice selection failed: "+field.Name);
   }
   foreach(var field in fields)field.SetValue(settings,false);
   Check(settings.PickRandomArrangement(random)==10,"Empty pool must safely select stock");
   settings.spawnRandomWA=true;settings.spawn4122=true;
   var state=new ConventionalArrangementState();state.BeginIdentity("A");state.SelectInitial(settings,random);
   Check(state.PendingId==30,"New spawn must select configured arrangement");
   settings.spawn4122=false;settings.spawn0100=true;state.SelectInitial(settings,random);
   Check(state.PendingId==30,"Pending spawn choice must not reroll when settings change");
   var saved=state.SavePayload();
   Check((int)saved["arrangement"]==30,"Save must retain pending choice before safe conversion");
   var reload=new ConventionalArrangementState();reload.BeginIdentity("A");
   Check(reload.LoadPayload(saved),"Valid payload rejected");reload.SelectInitial(settings,random);
   Check(reload.PendingId==30,"Reload rerolled the saved arrangement");
   reload.RecordApplied(19);Check(reload.PendingId==null && (int)reload.SavePayload()["arrangement"]==19,"Manual choice must replace pending restore");
   Check(!reload.BeginIdentity("A") && reload.AppliedId==19,"Same GUID must not reset selected choice");
   Check(reload.BeginIdentity("B") && !reload.HasSelection && reload.PendingId==null && reload.AppliedId==10,"Pool reuse leaked old GUID state");
   reload.SelectInitial(settings,random);Check(reload.PendingId==18,"New pool identity did not get new selection");
   var oldSave=new ConventionalArrangementState();oldSave.BeginIdentity("old");oldSave.LoadPayload(null);oldSave.SelectInitial(settings,random);
   Check(oldSave.PendingId==10,"Existing save without metadata must remain stock even when random enabled");
   for(int id=0;id<WheelArrangement.SupportedCount;id++){
    var value=new JObject{["version"]=1,["arrangement"]=id};var roundtrip=new ConventionalArrangementState();roundtrip.BeginIdentity("roundtrip");
    Check(roundtrip.LoadPayload(value) && roundtrip.PendingId==id,"Supported save ID rejected: "+id);
    roundtrip.RecordApplied(id);Check(JToken.DeepEquals(value,roundtrip.SavePayload()),"Saved ID did not round-trip: "+id);
   }
   foreach(var invalid in new JToken[]{new JObject{["version"]=2,["arrangement"]=30,["future"]=true},new JObject{["version"]=1,["arrangement"]=44},new JObject{["version"]=1,["arrangement"]=-1},new JObject{["version"]=1,["arrangement"]="30"},JToken.Parse("[1,2,3]"),JToken.Parse("{\"version\":1,\"arrangement\":9999999999999999999999999999999}")}){
    var value=new ConventionalArrangementState();value.BeginIdentity("invalid");
    Check(!value.LoadPayload(invalid) && value.PendingId==null,"Invalid/future metadata was applied");value.SelectInitial(settings,random);
    Check(JToken.DeepEquals(invalid,value.SavePayload()),"Unknown metadata must survive unchanged");
    value.RecordApplied(10);Check((int)value.SavePayload()["arrangement"]==10,"Explicit radio choice must replace unknown metadata");
   }
   var defaultPool=new Settings();var sampled=new HashSet<int>();
   for(int n=0;n<1000;n++){int chosen=defaultPool.PickRandomArrangement(random);sampled.Add(chosen);Check(WheelArrangement.IsConventionalId(chosen) && chosen!=36,"Default random selection left normal conventional catalogue");}
   Check(sampled.Contains(34) && sampled.Contains(35) && !sampled.Contains(36),"Default random selection must include both new normal choices and exclude experimental2-2-2");
   return checks;
  }
  public static int Main(){ System.Console.WriteLine(Run()); return 0; }
 }
}
'@
$harnessPath = Join-Path $output 'RandomLifecycleHarness.cs'
Set-Content -LiteralPath $harnessPath -Value $harness -Encoding utf8
$files = @($harnessPath,
    (Join-Path $PSScriptRoot 'RearrangedS282\Settings.cs'),
    (Join-Path $PSScriptRoot 'RearrangedS282\WheelArrangement.cs'),
    (Join-Path $PSScriptRoot 'RearrangedS282\ConventionalArrangementState.cs'))
$managed = 'B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed'
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><DefineConstants>B99_ALL_STANDARD</DefineConstants><NoWarn>1701;CS8632</NoWarn><NuGetAudit>false</NuGetAudit><StartupObject>RearrangedS282.RandomLifecycleHarness</StartupObject></PropertyGroup>
 <ItemGroup>$(($files | ForEach-Object { "<Compile Include=`"$_`"/>" }) -join '')</ItemGroup>
 <ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>$managed\Newtonsoft.Json.dll</HintPath></Reference></ItemGroup>
</Project>
"@
$projectPath = Join-Path $output 'RandomLifecycle.Validation.csproj'
Set-Content -LiteralPath $projectPath -Value $project -Encoding utf8
$result = & 'C:/Program Files/dotnet/dotnet.exe' run --project $projectPath --configuration Release
if ($LASTEXITCODE -ne 0) { $result; throw 'Random/catalogue/save-policy validation failed.' }
$count = [int]($result | Select-Object -Last 1)
$report = [ordered]@{ passed=$count;failed=0;source='Actual Settings, catalogue and ConventionalArrangementState';limitations='Offline pool/identity/save policy with managed attribute stubs; Unity spawn, safe deferral and native save hooks require in-game validation.' }
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'validation.json') -Encoding utf8
$report | ConvertTo-Json
