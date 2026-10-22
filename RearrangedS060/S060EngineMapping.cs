#if B99_S060_ARTICULATED
namespace RearrangedS060
{
    // Slot IDs are a saved/native contract, not physical position names. The
    // main slot is HP/front on Meyer, rear on Dockside and sole engine on stock.
    internal static class S060EngineMapping
    {
        internal const string MainId = "steamEngine", AuxiliaryId = "rs060FrontEngine";
        internal const string HighPressureId = MainId, LowPressureId = AuxiliaryId;
        internal static string SlotId(bool auxiliary) => auxiliary ? AuxiliaryId : MainId;
        internal static string PhysicalFrontId(S060EngineMode mode) =>
            mode == S060EngineMode.Dockside ? AuxiliaryId : MainId;
        internal static string PhysicalRearId(S060EngineMode mode) =>
            mode == S060EngineMode.Meyer ? AuxiliaryId : mode == S060EngineMode.Dockside ? MainId : null;
        internal static string ExhaustId(S060EngineMode mode) =>
            mode == S060EngineMode.Meyer ? LowPressureId : MainId;
        internal static string AdditionalExhaustId(S060EngineMode mode) =>
            mode == S060EngineMode.Dockside ? AuxiliaryId : null;
    }
}
#endif
