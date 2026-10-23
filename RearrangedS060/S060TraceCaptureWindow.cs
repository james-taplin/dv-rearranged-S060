namespace RearrangedS060
{
    // Passive diagnostic budget only. A new selected mode gets a fresh bounded
    // window; repeated notifications for the same mode cannot extend it.
    internal sealed class S060TraceCaptureWindow
    {
        internal const int MaximumSamples = 120;
        internal const float MaximumSeconds = 180, SampleInterval = .5f, SettlingSeconds = 3;
        private int? mode;
        private float nextSample, settlingUntil;
        internal int Samples { get; private set; }
        internal bool HasStarted { get; private set; }
        internal bool Stopped { get; private set; }
        internal float StartedAt { get; private set; }

        internal bool Select(int selectedMode, float now)
        {
            if (mode == selectedMode) return false;
            mode = selectedMode;
            Samples = 0; HasStarted = Stopped = false;
            StartedAt = nextSample = 0;
            settlingUntil = now + SettlingSeconds;
            return true;
        }
        internal string Limit(float now) => Samples >= MaximumSamples ? "sample limit" :
            HasStarted && now - StartedAt > MaximumSeconds ? "time limit" : null;
        internal bool TrySample(float now, bool regulatorOpen)
        {
            if (Stopped || mode == null || mode == 0 || Limit(now) != null || now < nextSample ||
                (!regulatorOpen && now >= settlingUntil)) return false;
            if (!HasStarted) { StartedAt = now; HasStarted = true; }
            nextSample = now + SampleInterval;
            Samples++;
            return true;
        }
        internal void Stop() => Stopped = true;
    }
}
