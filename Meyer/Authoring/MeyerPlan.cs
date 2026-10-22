// Fictional engineering candidates, not historical locomotive measurements.
// Shared by the CCL authoring recipe and headless geometry checks.
public static class MeyerPlan
{
    public const string CarId = "RS060_Meyer_0440";
    public const string CarName = "Rearranged S060 Meyer";
    public const double DriverRadius = 0.712128;  // measured S060 rolling radius
    public const double FlangeRadius = 1.533 / 2;
    public const double BogieCentre = 1.62;
    public const float FrontCentre = .90f;
    public const float RearCentre = -6.10f;
    public const float RearCouplerZ = -8.27f;
    public const float DeckCentreZ = -.075f;
    public const float DeckLength = 3.05f;
    public const float RearLampGlassZ = -1.87f;
    public const float NativeRearLampGlassZ = -4.22f;
    public const float RearBeamCentreZ = -1.80f;
    public const float RearBeamDepth = .16f;
    public const double AxlePitch = 1.60;
    public const float CrankRadius = RearrangedS060.S060SteamCalibration.Meyer.NativeCrankRadius;
    public const float PistonStroke = RearrangedS060.S060SteamCalibration.Meyer.NativeStroke;
    public const float MainRodLength = 1.15f;
    public const float CylinderZ = 1.38f;
    public const double TankCentreX = 1.05;
    public const double TankCentreY = 1.875;
    public const double TankWidth = .30;
    public const double TankHeight = .55;
    public const double TankLength = 1.30;
    public const string BodyMesh = "s060_body";
    public const string DriverMesh = "s060_Wheels_02";
    public const string BodyMaterial = "LocoS060Green_Body";
}
