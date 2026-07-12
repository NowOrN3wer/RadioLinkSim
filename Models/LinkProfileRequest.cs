namespace RadioLinkSim.Models;

public sealed record LinkProfileRequest(
    double LatA,
    double LonA,
    double LatB,
    double LonB,
    double StepMeters = 200.0);
