namespace RadioLinkSim.Services;

public interface IElevationProvider
{
    Task<IReadOnlyList<double>> GetElevationsAsync(
        IReadOnlyList<(double Latitude, double Longitude)> locations,
        CancellationToken cancellationToken);
}
