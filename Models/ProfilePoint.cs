namespace RadioLinkSim.Models;

public sealed record ProfilePoint(
    double Latitude,
    double Longitude,
    double DistanceFromAMeters,
    double ElevationMeters);
