namespace RadioLinkSim.Models.OpenElevation;

public sealed record LinkProfileResponse(
    double GreatCircleDistanceMeters,
    double EffectiveDistanceMeters,
    double StepMeters,
    int PointCount,
    IReadOnlyList<ProfilePoint> Points);
