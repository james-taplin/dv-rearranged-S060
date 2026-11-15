// Managed single-player regression fixtures do not start an MP session.
// Multiplayer policy is exercised separately with the production sync source.
namespace RearrangedS060
{
    internal static class S060MultiplayerSync
    {
        internal static bool IsSimulationAuthority => true;
        internal static bool IsClient => false;
        internal static bool ApplyingRemote => false;
        internal static bool BeforeSwitch(object car) => true;
    }
}
