#if B99_DOCKSIDE
using System;
using System.Collections.Generic;
using System.Linq;
using LocoSim.Implementations;

namespace RearrangedS060
{
    // Native two-cylinder engines already quarter cylinder 1 by +.25 turn.
    // Stagger the front unit by .125 turn for eight interleaved exhaust beats.
    // Seed once per new native flow; presentation/pool rebinding must not reset it.
    internal static class DocksidePhasing
    {
        internal const float FrontOffset = .125f;
        internal static void Seed(IReadOnlyList<SimComponent> components)
        {
            foreach (string id in new[] { "steamEngine", "meyerLpEngine" })
            {
                var engine = components.OfType<ReciprocatingSteamEngine>().Single(e => e.id == id);
                float phase = id == "steamEngine" ? 0 : FrontOffset;
                engine.crankRotation = engine.prevCrankRotation = phase;
                engine.crankRotationReadOut.Value = phase;
            }
        }
    }
}
#endif
