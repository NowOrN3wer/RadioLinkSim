namespace RadioLinkSim.Models.OpenElevation;

public sealed record OpenElevationResponse(
    IReadOnlyList<OpenElevationResult> Results);
