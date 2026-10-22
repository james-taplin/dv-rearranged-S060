// Fictional fit candidates. Wheel width/gauge and the S060 body are not scaled.
public static class DocksidePlan
{
    public const string CarId = "RS060_Franklin_Dockside_0440";
    public const string CarName = "Rearranged S060 Franklin Dockside";
    public const float DriverRadius = RearrangedS060.S060SteamCalibration.Dockside.DriverRadius;
    public const float NativeDriverRadius = RearrangedS060.S060SteamCalibration.Dockside.NativeDriverRadius;
    public const float BogieCentre = 1.40f;
    public const float AxlePitch = 1.20f;
    public const float Bore = RearrangedS060.S060SteamCalibration.Dockside.Bore;
    // Measured native S060 wheel's crank-pin cap centre, not a guessed throw.
    public const float NativeCrankRadius = RearrangedS060.S060SteamCalibration.Dockside.NativeCrankRadius;
    public const float CrankRadius = RearrangedS060.S060SteamCalibration.Dockside.CrankRadius;
    public const float Stroke = RearrangedS060.S060SteamCalibration.Dockside.AuthoredStroke;
    public const float RodPlaneX = RearrangedS060.S060OilCupGeometry.CouplingRodPlaneX;
    public const float MainRodPlaneX = RearrangedS060.S060OilCupGeometry.MainRodPlaneX;
    public const float PistonAxisX = RearrangedS060.S060OilCupGeometry.PistonAxisX;
    public const float MainRodLength = 1.40f;
    public const float CylinderZ = 1.60f;
    public const float FranklinZScale = (CylinderZ + AxlePitch / 2) / (9.49f - 6.274f);
    public const float ChestLitres = RearrangedS060.S060SteamCalibration.Dockside.ChestLitres;
    public const float MaxFlow = RearrangedS060.S060SteamCalibration.Dockside.MaxFlow;
}
