param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot 'build\runtime-trial-1.4.1\bin\RearrangedS282.dll'),
    [string]$OutputPath = (Join-Path $PSScriptRoot 'build\front-coupling-validation\validation.json')
)
$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot 'RearrangedS282\WheelRearranger.Conventional.cs'
$production = Get-Content -LiteralPath $sourcePath -Raw
$start = $production.IndexOf('private void CaptureFrontCouplingMounts(TrainCar car)')
if ($start -lt 0) { throw 'Production mount capture method missing.' }
$body = $production.IndexOf('{', $start)
$depth = 1
$end = $body + 1
while ($depth -gt 0 -and $end -lt $production.Length) {
    if ($production[$end] -eq '{') { $depth++ }
    if ($production[$end] -eq '}') { $depth-- }
    $end++
}
if ($depth -ne 0) { throw 'Could not extract production mount capture method.' }
$method = $production.Substring($start, $end - $start)
$catalogue = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'RearrangedS282\WheelArrangement.cs') -Raw
$catalogue = $catalogue.Substring($catalogue.IndexOf('namespace RearrangedS282'))
$stubs = @'
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace UnityEngine {
 public struct Vector3 {
  public float x,y,z;
  public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
 }
 public class Transform {
  public Vector3 localPosition;
  public object parent=new object();
  public int rotation=19,knobState=7;
  public bool active=true,connected=true;
 }
}
public class VisualCouplerInit { public Transform chain,hoses; }
public class Coupler { public VisualCouplerInit visualCoupler; }
public class TrainCar {
 public Coupler frontCoupler;
 public float physicalPilot;
}
namespace FrontCouplingRegression {
 public sealed class GeometryState {
  private readonly List<Action> restore=new List<Action>();
  public GeometryState(TrainCar car,bool captureMounts){
   // Model the existing car hierarchy snapshot. Native interior mounts live
   // outside that hierarchy; the extracted production method adds them.
   var pilot=car.physicalPilot;
   restore.Add(()=>car.physicalPilot=pilot);
   if(captureMounts)CaptureFrontCouplingMounts(car);
  }
  public void Restore(){foreach(var action in restore)action();}
'@
$fixture = @'
 }
 public static class Harness {
  static int checks;
  static void Check(bool condition,string label){if(!condition)throw new Exception(label);checks++;}
  static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
  static TrainCar Car(){return new TrainCar {frontCoupler=new Coupler {visualCoupler=new VisualCouplerInit {
   chain=new Transform {localPosition=new Vector3(0,.97f,11.64f)},
   hoses=new Transform {localPosition=new Vector3(0,1.05f,11.98f)}
  }}};}
  static void MovePilot(TrainCar car,float delta){
   // Installed LMS MovePilot applies the same additive z delta to physical
   // pilot/anchor and both separate-interior mount roots. IK is native-owned.
   car.physicalPilot+=delta;
   foreach(var mount in new[]{car.frontCoupler.visualCoupler.chain,car.frontCoupler.visualCoupler.hoses}){
    var p=mount.localPosition;p.z+=delta;mount.localPosition=p;
   }
  }
  static void CheckMounts(TrainCar car,float offset){
   var visual=car.frontCoupler.visualCoupler;
   Check(Near(visual.chain.localPosition.z,11.64f+offset),"Chain mount drifted");
   Check(Near(visual.hoses.localPosition.z,11.98f+offset),"Hose mount drifted");
   Check(Near(visual.chain.localPosition.y,.97f)&&Near(visual.hoses.localPosition.y,1.05f),"Mount height changed");
   Check(Near(car.physicalPilot,offset),"Physical pilot disagrees with visual mounts");
  }
  public static int Run(){
   var old=Car();var oldSnapshot=new GeometryState(old,false);
   for(int i=0;i<7;i++){oldSnapshot.Restore();MovePilot(old,-.47f);}
   Check(Near(old.frontCoupler.visualCoupler.chain.localPosition.z,11.64f-3.29f),"Old seven-layout drift was not reproduced");
   oldSnapshot.Restore();
   Check(!Near(old.frontCoupler.visualCoupler.hoses.localPosition.z,11.98f),"Old stock restore unexpectedly restored separate interior");

   var car=Car();var stock=new GeometryState(car,true);var prepared=new GeometryState(car,true);
   var chain=car.frontCoupler.visualCoupler.chain;var hoses=car.frontCoupler.visualCoupler.hoses;
   var chainParent=chain.parent;var hoseParent=hoses.parent;
   chain.rotation=31;chain.knobState=3;chain.active=false;chain.connected=false;
   hoses.rotation=42;hoses.knobState=4;hoses.active=false;hoses.connected=false;
   for(int cycle=0;cycle<4;cycle++)for(int id=0;id<RearrangedS282.WheelArrangement.SupportedCount;id++){
    prepared.Restore();
    float offset=RearrangedS282.WheelArrangement.WheelArrangementNames[id].StartsWith("0-")?-.47f:0f;
    if(offset!=0)MovePilot(car,offset);
    CheckMounts(car,offset);
   }
   stock.Restore();CheckMounts(car,0);
   Check(ReferenceEquals(chain.parent,chainParent)&&ReferenceEquals(hoses.parent,hoseParent),"Native parent ownership changed");
   Check(chain.rotation==31&&hoses.rotation==42,"Native rotation was rewound");
   Check(chain.knobState==3&&hoses.knobState==4&&!chain.connected&&!hoses.connected,"Native coupling/IK state was rewound");
   Check(!chain.active&&!hoses.active,"Native activity state was rewound");
   // Stock restoration is also the existing rollback and pool path.
   MovePilot(car,-.47f);stock.Restore();stock.Restore();CheckMounts(car,0);
   var noVisuals=new TrainCar();new GeometryState(noVisuals,true).Restore();checks++;
   car.frontCoupler.visualCoupler.chain=null;car.frontCoupler.visualCoupler.hoses=null;
   new GeometryState(car,true).Restore();checks++;
   return checks;
  }
 }
}
'@
Add-Type -TypeDefinition ($stubs + "`n" + $method + "`n" + $fixture + "`n" + $catalogue)
$count = [FrontCouplingRegression.Harness]::Run()
Add-Type -Path (Join-Path $PSScriptRoot 'lib\AssetStudioModCLI_net472_win32_64\Mono.Cecil.dll')
$module = [Mono.Cecil.ModuleDefinition]::ReadModule($AssemblyPath)
try {
    $geometry = $module.GetTypes() | Where-Object FullName -EQ 'RearrangedS282.WheelRearranger/GeometryState'
    $constructor = $geometry.Methods | Where-Object IsConstructor
    if (-not ($constructor.Body.Instructions.Operand -match 'CaptureFrontCouplingMounts')) { throw 'Compiled geometry constructor does not capture mounts.' }
    $capture = $geometry.Methods | Where-Object Name -EQ 'CaptureFrontCouplingMounts'
    foreach ($field in @('VisualCouplerInit::chain','VisualCouplerInit::hoses')) {
        if (-not ($capture.Body.Instructions.Operand -match [regex]::Escape($field))) { throw "Compiled native mount reference missing: $field" }
    }
    $report = [ordered]@{
        passed=$true;checks=$count;version=$module.Assembly.Name.Version.ToString()
        dllSHA256=(Get-FileHash -LiteralPath $AssemblyPath).Hash
        sourceSHA256=(Get-FileHash -LiteralPath $sourcePath).Hash
        oldCounterexample='Seven no-leading layouts move separate-interior mounts back 3.29m; old stock snapshot leaves drift.'
        fixed='Four complete catalogue sweeps, stock/pool-style restoration and repeated restore preserve mount positions and leave native IK/coupling/activity/parent/rotation state untouched.'
        limitations='Extracted actual production mount-capture helper with Unity/native hierarchy stand-ins and an explicit LMS additive MovePilot model; compiled constructor/native field inspection. No in-game coupling, physics, IK or visual pass.'
    }
    New-Item -ItemType Directory -Path (Split-Path $OutputPath -Parent) -Force | Out-Null
    $report | ConvertTo-Json | Set-Content -LiteralPath $OutputPath -Encoding utf8
    $report | ConvertTo-Json
} finally { $module.Dispose() }
