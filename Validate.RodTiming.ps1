param(
 [string]$AssemblyPath=(Join-Path $PSScriptRoot 'build\rod-sync-candidate\bin\RearrangedS282.dll'),
 [string]$OutputPath=(Join-Path $PSScriptRoot 'build\rod-sync-candidate\rod-timing-validation.json'),
 [double[]]$SpeedsKmh=@(20,60,100,120,200,376,462),
 [double[]]$FrameRates=@(20,30,60,144)
)
$ErrorActionPreference='Stop'
$AssemblyPath=(Resolve-Path -LiteralPath $AssemblyPath).Path
$root=$PSScriptRoot
$before=Get-Content -LiteralPath (Join-Path $root 'evidence\beta1-high-speed-rod-report\PoweredWheelRotater.before.cs') -Raw
$before=$before.Replace('class PoweredWheelRotater','class OldPoweredWheelRotater')
$after=Get-Content -LiteralPath (Join-Path $root 'RearrangedS282\PoweredWheelRotater.cs') -Raw
$stubs=@"
using System;
namespace UnityEngine {
 public struct Vector3 { public double x,y,z; public Vector3(double a,double b,double c){x=a;y=b;z=c;} }
 public struct Quaternion {
  public double x,y,z,w;
  public static Quaternion Angle(double degrees){double r=degrees*Math.PI/360;return new Quaternion{x=Math.Sin(r),w=Math.Cos(r)};}
 }
 public class Transform {
  public Quaternion localRotation=Quaternion.Angle(0);
  public Vector3 localEulerAngles {get{return new Vector3(Math.Atan2(localRotation.x,localRotation.w)*360/Math.PI,0,0);}set{localRotation=Quaternion.Angle(value.x);}}
 }
 public class GameObject { public Transform transform=new Transform(); }
 public class MonoBehaviour { public Transform transform=new Transform(); }
}
namespace RodTimingRegression {
 public static class Harness {
  public static string Run(double[] speedsKmh,double[] frameRates){
   int samples=0,lagSamples=0;
   foreach(double speed in speedsKmh) foreach(double fps in frameRates) foreach(int direction in new[]{-1,1}){
    var source=new UnityEngine.GameObject();
    var old=new RearrangedS282.OldPoweredWheelRotater();old.Init(source);
    var current=new RearrangedS282.PoweredWheelRotater();current.Init(source);
    double step=direction*(speed/3.6)/(.6096*2*Math.PI)*360/fps;
    for(int frame=1;frame<=120;frame++){
     // Normal player loop contract: Update -> Animator pose -> LateUpdate -> render.
     old.Update();
     source.transform.localRotation=UnityEngine.Quaternion.Angle(frame*step);
     current.LateUpdate();
     var expected=source.transform.localRotation;var actual=current.transform.localRotation;
     if(actual.x!=expected.x || actual.y!=expected.y || actual.z!=expected.z || actual.w!=expected.w)throw new Exception("Added wheel did not match the rendered source quaternion");
     var previous=old.transform.localRotation;
     double dot=Math.Abs(previous.x*expected.x+previous.w*expected.w);
     if(dot<.999999)lagSamples++;
     samples++;
    }
   }
   if(lagSamples!=samples)throw new Exception("Expected old callback to lag in every moving frame");
   var unbound=new RearrangedS282.PoweredWheelRotater();unbound.LateUpdate();unbound.Init(null);unbound.LateUpdate();
   return samples+" simulated rendered poses match; old Update lags in "+lagSamples+" poses; unbound source handled";
  }
 }
}
"@
# Compile actual before/after helper bodies, not a duplicated implementation.
$beforeBody=$before.Substring($before.IndexOf('namespace RearrangedS282'))
$afterBody=$after.Substring($after.IndexOf('namespace RearrangedS282'))
$source=$stubs+"`nnamespace RearrangedS282 { using UnityEngine; }`n"+$beforeBody.Replace('namespace RearrangedS282','namespace RearrangedS282').Replace("namespace RearrangedS282`r`n{","namespace RearrangedS282`r`n{`r`nusing UnityEngine;")+"`n"+$afterBody.Replace("namespace RearrangedS282`r`n{","namespace RearrangedS282`r`n{`r`nusing UnityEngine;")
# Source line endings can differ; put a shared global using at the start.
$source="using UnityEngine;`n"+$source
Add-Type -TypeDefinition $source
$result=[RodTimingRegression.Harness]::Run($SpeedsKmh,$FrameRates)
Add-Type -Path (Join-Path $root 'lib\AssetStudioModCLI_net472_win32_64\Mono.Cecil.dll')
$m=[Mono.Cecil.ModuleDefinition]::ReadModule($AssemblyPath)
try {
 $type=$m.GetTypes()|Where-Object FullName -EQ 'RearrangedS282.PoweredWheelRotater'
 if($type.Methods.Name -contains 'Update' -or $type.Methods.Name -notcontains 'LateUpdate'){throw 'Compiled callback is not exclusively LateUpdate'}
 $ops=@(($type.Methods|Where-Object Name -EQ LateUpdate).Body.Instructions|ForEach-Object{[string]$_.Operand})
 if(-not($ops -match 'get_localRotation') -or -not($ops -match 'set_localRotation') -or ($ops -match 'EulerAngles')){throw 'Compiled synchronization does not copy native quaternions'}
 $setup=($m.GetTypes()|Where-Object FullName -EQ 'RearrangedS282.WheelRearranger').Methods|Where-Object Name -EQ 'SetupDrivers'
 $bindings=@($setup.Body.Instructions|Where-Object { [string]$_.Operand -match 'PoweredWheelRotater::Init\(' }).Count
 if($bindings -ne 4){throw "Expected four added-wheel synchronization bindings, found $bindings"}
 $version=$m.Assembly.Name.Version.ToString()
}finally{$m.Dispose()}
$report=[ordered]@{passed=$true;checkedAt=(Get-Date).ToString('o');assembly=$AssemblyPath;assemblySha256=(Get-FileHash -LiteralPath $AssemblyPath -Algorithm SHA256).Hash;version=$version;speedsKmh=$SpeedsKmh;frameRates=$FrameRates;simulation=$result;compiledCallback='LateUpdate';addedWheelBindings=$bindings;limitations='Actual source with Unity stand-ins for the Update/Animator/LateUpdate sequence, plus compiled IL inspection. Extreme speeds exercise pose synchronization only, not attainable speed, Unity animation scheduling or rendered rod attachment. No Unity/game high-speed reproduction performed.'}
$outputDirectory=Split-Path -Parent $OutputPath
if($outputDirectory -and -not(Test-Path -LiteralPath $outputDirectory)){New-Item -ItemType Directory -Path $outputDirectory -Force|Out-Null}
$report|ConvertTo-Json|Set-Content -LiteralPath $OutputPath -Encoding utf8
$report|ConvertTo-Json
