namespace RadioLinkSim.Models.OpenElevation;

public sealed record OpenElevationRequest(
    IReadOnlyList<OpenElevationLocation> Locations);
