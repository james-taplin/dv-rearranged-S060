$ErrorActionPreference = 'Stop'
# Offline checks of the Stage 2 curve-drag model (RearrangedS282\CurveDragModel.cs).
# Geometry is the measured driver body z from the 1.7.4 capture
# (power-20261019-182301-729.jsonl). Runtime forces and audio need in-game testing.
$output = Join-Path $PSScriptRoot 'build\curve-drag-validation'
New-Item -ItemType Directory -Force $output | Out-Null
$harness = @'
using System;
using System.Collections.Generic;
using static RearrangedS282.CurveDragModel;
namespace RearrangedS282
{
 public static class CurveDragHarness
 {
  static int checks;
  static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; }
  static Axle[] G(double[] z, bool[] f) => WithPlay(z, f);
  public static int Main()
  {
   double weight = 111000 * 9.81;
   var stock = new[] { G(StockZ, StockFlanged) };
   var f4144 = new[] { G(new[] { 8.686, 7.511, 6.436, 5.362, 4.293, 3.221, 2.149 }, new[] { true, false, false, false, false, true, true }) };
   var f4144AllFlanged = new[] { G(new[] { 8.686, 7.511, 6.436, 5.362, 4.293, 3.221, 2.149 }, new[] { true, true, true, true, true, true, true }) };
   var mallet = new[] { G(new[] { 8.443, 7.28, 6.117 }, new[] { true, false, false }), G(new[] { 3.71, 2.545, 1.38 }, new[] { true, false, false }) };
   var duplex = new[] { G(new[] { 8.0, 6.3 }, new[] { true, false }), G(new[] { 4.75, 3.2 }, new[] { true, false }) };
   var triplex = new[] { G(new[] { 7.455 }, new[] { true }), G(new[] { 4.254 }, new[] { true }), G(new[] { 1.05 }, new[] { true }) };
   var radii = new[] { 35.0, 50, 60, 80, 100, 150, 200, 300, 400, 600, 1000, 2500, 5000 };
   foreach (var layout in new[] { stock, f4144, mallet, duplex, triplex })
    Check(ExtraN(layout, 0, weight / 11, weight) == 0 && ExtraN(layout, double.NaN, weight / 11, weight) == 0, "Straight or unmeasured track adds nothing");
   double previous = 0;
   for (int i = radii.Length - 1; i >= 0; i--)
   {
    double k = 1 / radii[i], r = radii[i];
    Check(ExtraN(stock, k, weight / 6, weight) == 0, $"Stock 2-8-2 gets no extra drag at R{r}");
    Check(ExtraN(stock, -k, weight / 6, weight) == 0, $"Curve direction irrelevant for stock at R{r}");
    double x4144 = ExtraN(f4144, k, weight / 11, weight);
    Check(x4144 >= 0 && x4144 == ExtraN(f4144, -k, weight / 11, weight), $"4-14-4 extra non-negative and direction-independent at R{r}");
    Check(x4144 + 1e-9 >= previous, $"4-14-4 extra rises as the curve tightens (R{r})");
    previous = x4144;
    Check(ExtraN(mallet, k, weight / 8, weight) <= x4144 + 1e-9, $"Mallet never drags more than the 4-14-4 at R{r}");
    Check(ExtraN(triplex, k, weight / 5, weight) == 0, $"Triplex single-axle engines self-steer at R{r}");
    Check(ExtraN(duplex, k, weight / 6, weight) == 0, $"2-4-4-2 short engines curve no worse than stock at R{r}");
    Check(Groups(f4144AllFlanged, k, weight / 11).FlangeN + 1e-9 >= Groups(f4144, k, weight / 11).FlangeN, $"Blind drivers never add flange drag at R{r}");
    Check(Groups(stock, k, weight / 6).FlangeN == 0, $"Two flanged drivers always fit the curve at R{r}");
    double rockl = (r >= 300 ? 650 / (r - 55) : 500 / (r - 30)) * weight / 1000;
    Check(x4144 <= 1.5 * rockl + 500, $"4-14-4 extra stays within 1.5x Rockl whole-car resistance at R{r}");
    if (r >= 400) Check(x4144 < 500, $"4-14-4 extra negligible on broad curves (R{r})");
   }
   Check(ExtraN(f4144, 1 / 80.0, weight / 11, weight) > 2500, "4-14-4 pays noticeably on an 80 m curve");
   Check(ExtraN(f4144, 1 / 50.0, weight / 11, weight) > 8000, "4-14-4 pays heavily on a 50 m curve");
   var play = G(new[] { 1.0, 3.0, 2.0, 5.0, 4.0 }, new[] { true, true, true, true, true });
   Check(play[0].Z == 5 && play[4].Z == 1, "Axles ordered front to back");
   Check(Math.Abs(play[0].FreeM - (FlangewayM + SideplayM + LeadDriverExtraM)) < 1e-12 && Math.Abs(play[1].FreeM - (FlangewayM + SideplayM)) < 1e-12, "Lead driver of a long group gets the lateral-motion allowance");
   Check(Math.Abs(G(StockZ, StockFlanged)[0].FreeM - (FlangewayM + SideplayM)) < 1e-12, "Four-coupled lead driver gets no allowance");
   Check(SquealLevel(5000, weight, 0) == 0 && SquealLevel(5000, weight, .4) == 0, "No squeal at standstill or creeping");
   Check(SquealLevel(0, weight, 10) == 0, "No squeal without extra drag");
   Check(SquealLevel(1e9, weight, 10) == 1, "Squeal capped at 1");
   Check(SquealLevel(10000, weight, 10) > SquealLevel(5000, weight, 10), "Squeal rises with extra drag");
   foreach (var r in new[] { 50.0, 80, 100, 150, 200, 400 })
    Console.Error.WriteLine($"R{r,5}: 4-14-4 +{ExtraN(f4144, 1 / r, weight / 11, weight) / 1000:F2} kN, Mallet +{ExtraN(mallet, 1 / r, weight / 8, weight) / 1000:F2} kN, squeal {SquealLevel(ExtraN(f4144, 1 / r, weight / 11, weight), weight, 8):F3}");
   bool threw = false;
   try { WithPlay(new[] { 1.0 }, new[] { true, false }); } catch (ArgumentException) { threw = true; }
   Check(threw, "Mismatched driver data rejected");
   Console.WriteLine(checks);
   return 0;
  }
 }
}
'@
$harnessPath = Join-Path $output 'CurveDragHarness.cs'
Set-Content -LiteralPath $harnessPath -Value $harness -Encoding utf8
$model = Join-Path $PSScriptRoot 'RearrangedS282\CurveDragModel.cs'
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><DefineConstants>CURVE_MODEL_TEST</DefineConstants><NuGetAudit>false</NuGetAudit><StartupObject>RearrangedS282.CurveDragHarness</StartupObject></PropertyGroup>
 <ItemGroup><Compile Include="$harnessPath"/><Compile Include="$model"/></ItemGroup>
</Project>
"@
$projectPath = Join-Path $output 'CurveDrag.Validation.csproj'
Set-Content -LiteralPath $projectPath -Value $project -Encoding utf8
$result = & 'C:/Program Files/dotnet/dotnet.exe' run --project $projectPath --configuration Release
if ($LASTEXITCODE -ne 0) { $result; throw 'Curve drag validation failed.' }
$count = [int]($result | Select-Object -Last 1)
$report = [ordered]@{ passed=$true;checks=$count;source='RearrangedS282\CurveDragModel.cs with geometry measured in the 1.7.4 capture';limitations='Model maths only; bogie forces, wheelslip gating and squeal audio need in-game testing.' }
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'validation.json') -Encoding utf8
$report | ConvertTo-Json
