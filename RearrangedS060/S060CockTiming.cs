#if B99_S060_ARTICULATED
using System;
using DV.Simulation.Controllers;
using HarmonyLib;
using LocoSim.Implementations;

namespace RearrangedS060
{
    // B99 sends three different event values into UpdateCylCockParticles, whose
    // argument is interpreted as crank phase. Correct only our owned readers;
    // native curves, valve masks, reverser and flow remain entirely native.
    internal sealed class S060CockTiming : IDisposable
    {
        private readonly CylinderCockParticlePortReader reader;
        private readonly Port crank, reverser, flow, inlet;
        private readonly Action<float> native, refresh;
        private bool disposed;
        internal static S060CockTiming Bind(CylinderCockParticlePortReader reader) => new S060CockTiming(reader);
        private S060CockTiming(CylinderCockParticlePortReader reader)
        {
            this.reader = reader;
            crank = reader.crankRotationPort; reverser = reader.reverserPort; flow = reader.cylinderCockFlowNormalizedPort;
            inlet = reader.cylindersInletValveOpenPort;
            if (crank == null || reverser == null || flow == null || inlet == null) throw new InvalidOperationException("Owned cylinder-cock reader must initialize before timing enrollment.");
            native = AccessTools.MethodDelegate<Action<float>>(AccessTools.Method(typeof(CylinderCockParticlePortReader), "UpdateCylCockParticles"), reader);
            refresh = _ => native(crank.Value);
            crank.ValueUpdatedInternally -= native; reverser.ValueUpdatedInternally -= native; flow.ValueUpdatedInternally -= native;
            crank.ValueUpdatedInternally += refresh; reverser.ValueUpdatedInternally += refresh; flow.ValueUpdatedInternally += refresh;
            // Native slots publish phase before their valve mask. Refresh the
            // final mask too, even if phase/flow stay fixed at a stopped engine.
            inlet.ValueUpdatedInternally += refresh;
            try { refresh(0); }
            catch { Dispose(); throw; }
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            crank.ValueUpdatedInternally -= refresh; reverser.ValueUpdatedInternally -= refresh; flow.ValueUpdatedInternally -= refresh;
            inlet.ValueUpdatedInternally -= refresh;
            // Return native ownership before controller.Deinit. If native
            // teardown already ran, do not resurrect its subscriptions.
            if (reader != null && reader.crankRotationPort == crank)
            { crank.ValueUpdatedInternally += native; reverser.ValueUpdatedInternally += native; flow.ValueUpdatedInternally += native; }
        }
    }
}
#endif
