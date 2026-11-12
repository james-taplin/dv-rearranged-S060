using System;
using RearrangedS060;

internal static class Program
{
    private static int checks;
    private static void Assert(bool value, string label)
    { if (!value) throw new Exception("FAIL: " + label); checks++; }
    private static void Main()
    {
        var window = new S060TraceCaptureWindow();
        Assert(!window.TrySample(0, true), "unselected owner cannot capture");
        Assert(window.Select(1, 0), "Meyer arms a window");
        Assert(window.TrySample(0, false), "closed-regulator conversion settling is captured");
        Assert(!window.TrySample(.49f, true), "cadence never exceeds two Hz");
        Assert(window.TrySample(.5f, false), "second settling sample");
        Assert(!window.HasStarted && window.Limit(3600) == null,
            "settling alone does not start or exhaust the powered-test time budget");
        Assert(!window.Select(1, 2), "same-mode notification does not rearm");
        Assert(!window.TrySample(3, false), "closed-regulator settling ends after three seconds");
        Assert(window.TrySample(3, true), "normal open-regulator sampling resumes");
        Assert(window.Samples == 3, "only actual samples consume budget");
        Assert(window.HasStarted && window.StartedAt == 3, "first powered sample starts the active clock");
        Assert(window.Limit(183) == null, "exact active time boundary is retained");
        Assert(window.Limit(183.01f) == "time limit", "active wall time remains bounded while regulator closed");
        Assert(!window.TrySample(184, true), "timed-out window does not capture");
        Assert(window.Select(2, 185), "Dockside re-arms after Meyer timeout");
        Assert(window.Samples == 0 && !window.HasStarted && !window.Stopped, "new mode resets only diagnostic budget");
        Assert(window.TrySample(185, false), "Dockside captures without waiting for steam");
        window.Stop();
        Assert(!window.TrySample(186, true), "disabled or disposed owner stops capture");
        Assert(!window.Select(2, 187) && window.Stopped, "duplicate selection cannot unstop a session");
        Assert(window.Select(0, 188), "stock transition ends articulated session");
        Assert(!window.TrySample(188, true), "stock never opens an articulated sample stream");
        Assert(window.Select(1, 189), "stock back to Meyer gets a fresh session even between updates");
        for (int i = 0; i < S060TraceCaptureWindow.MaximumSamples; i++)
            Assert(window.TrySample(189 + i * .5f, true), "bounded sample " + i);
        Assert(window.Samples == 120 && window.Limit(249) == "sample limit", "sample limit remains 120");
        Assert(!window.TrySample(249, true), "no 121st sample");
        Assert(!window.Select(1, 250) && !window.TrySample(250, true), "same-mode repeated event cannot bypass sample limit");
        Assert(window.Select(2, 251) && window.TrySample(251, true), "Meyer sample exhaustion does not suppress later Dockside diagnostics");
        Assert(window.Samples == 1 && window.StartedAt == 251, "session time and sample count restart together");

        var warmup = new S060TraceCaptureWindow(); warmup.Select(3, 0);
        for (int i = 0; i < 6; i++) Assert(warmup.TrySample(i * .5f, false), "unchanged two Hz conversion settling " + i);
        Assert(!warmup.TrySample(3, false) && !warmup.TrySample(3600, false), "long warmup emits no further idle samples");
        Assert(warmup.Samples == 6 && !warmup.HasStarted && warmup.Limit(3600) == null, "one-hour warmup keeps six samples and an unspent powered budget");
        Assert(warmup.TrySample(3600, true) && warmup.StartedAt == 3600 && warmup.HasStarted, "Shay first powered sample remains available after long warmup");
        Assert(!warmup.TrySample(3600.49f, true) && warmup.TrySample(3600.5f, true), "powered cadence remains bounded after warmup");
        Assert(!warmup.Select(3, 3700) && warmup.StartedAt == 3600, "duplicate Shay selection cannot restart active time");
        Assert(warmup.Limit(3780) == null && warmup.Limit(3780.01f) == "time limit", "powered window ends after 180 seconds including closed-regulator time");
        Assert(!warmup.TrySample(3781, true), "expired active session cannot reopen by regulator cycling");
        Assert(warmup.Select(1, 3782) && !warmup.HasStarted && warmup.Samples == 0, "later arrangement gets fresh settling and powered budgets");
        Assert(warmup.TrySample(3782, false) && !warmup.HasStarted && warmup.TrySample(4000, true), "rollover retains delayed powered start");
        Console.WriteLine("PASS: " + checks + " diagnostic budget assertions across initial, settling, timeout, disabled, stock and rollover scenarios.");
    }
}
