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
        Assert(!window.Select(1, 2), "same-mode notification does not rearm");
        Assert(!window.TrySample(3, false), "closed-regulator settling ends after three seconds");
        Assert(window.TrySample(3, true), "normal open-regulator sampling resumes");
        Assert(window.Samples == 3, "only actual samples consume budget");
        Assert(window.Limit(180) == null, "exact time boundary is retained");
        Assert(window.Limit(180.01f) == "time limit", "wall time remains bounded while regulator closed");
        Assert(!window.TrySample(181, true), "timed-out window does not capture");
        Assert(window.Select(2, 182), "Dockside re-arms after Meyer timeout");
        Assert(window.Samples == 0 && !window.HasStarted && !window.Stopped, "new mode resets only diagnostic budget");
        Assert(window.TrySample(182, false), "Dockside captures without waiting for steam");
        window.Stop();
        Assert(!window.TrySample(183, true), "disabled or disposed owner stops capture");
        Assert(!window.Select(2, 184) && window.Stopped, "duplicate selection cannot unstop a session");
        Assert(window.Select(0, 185), "stock transition ends articulated session");
        Assert(!window.TrySample(185, true), "stock never opens an articulated sample stream");
        Assert(window.Select(1, 186), "stock back to Meyer gets a fresh session even between updates");
        for (int i = 0; i < S060TraceCaptureWindow.MaximumSamples; i++)
            Assert(window.TrySample(186 + i * .5f, true), "bounded sample " + i);
        Assert(window.Samples == 120 && window.Limit(246) == "sample limit", "sample limit remains 120");
        Assert(!window.TrySample(246, true), "no 121st sample");
        Assert(!window.Select(1, 247) && !window.TrySample(247, true), "same-mode repeated event cannot bypass sample limit");
        Assert(window.Select(2, 248) && window.TrySample(248, true), "Meyer sample exhaustion does not suppress later Dockside diagnostics");
        Assert(window.Samples == 1 && window.StartedAt == 248, "session time and sample count restart together");
        Console.WriteLine("PASS: " + checks + " diagnostic budget assertions across initial, settling, timeout, disabled, stock and rollover scenarios.");
    }
}
