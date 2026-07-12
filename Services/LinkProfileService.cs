using RadioLinkSim.Models;
using RadioLinkSim.Models.OpenElevation;

namespace RadioLinkSim.Services;

public sealed class LinkProfileService(
    GeodesyService geodesyService,
    IElevationProvider elevationProvider)
{
    public async Task<LinkProfileResponse> CreateProfileAsync(
        LinkProfileRequest request,
        CancellationToken cancellationToken)
    {
        var greatCircleDistanceMeters = geodesyService.CalculateDistanceMeters(
            request.LatA,
            request.LonA,
            request.LatB,
            request.LonB);

        var initialBearingRadians = geodesyService.CalculateInitialBearingRadians(
            request.LatA,
            request.LonA,
            request.LatB,
            request.LonB);

        var pathPoints = CreatePathPoints(
            request,
            greatCircleDistanceMeters,
            initialBearingRadians);

        var elevationLocations = pathPoints
            .Select(point => (point.Latitude, point.Longitude))
            .ToArray();

        var elevations = await elevationProvider.GetElevationsAsync(
            elevationLocations,
            cancellationToken);

        var profilePoints = new ProfilePoint[pathPoints.Count];

        for (var index = 0; index < pathPoints.Count; index++)
        {
            var pathPoint = pathPoints[index];

            profilePoints[index] = new ProfilePoint(
                pathPoint.Latitude,
                pathPoint.Longitude,
                pathPoint.DistanceFromAMeters,
                elevations[index]);
        }

        var effectiveDistanceMeters = CalculateEffectiveDistance(profilePoints);

        return new LinkProfileResponse(
            GreatCircleDistanceMeters: greatCircleDistanceMeters,
            EffectiveDistanceMeters: effectiveDistanceMeters,
            StepMeters: request.StepMeters,
            PointCount: profilePoints.Length,
            Points: profilePoints);
    }

    private IReadOnlyList<PathPoint> CreatePathPoints(
        LinkProfileRequest request,
        double greatCircleDistanceMeters,
        double initialBearingRadians)
    {
        var fullStepCount = (int)Math.Floor(greatCircleDistanceMeters / request.StepMeters);
        var points = new List<PathPoint>(capacity: fullStepCount + 2) { new(request.LatA, request.LonA, 0.0) };

        for (var index = 1; index <= fullStepCount; index++)
        {
            var distanceFromAMeters = index * request.StepMeters;

            // If the distance is an exact multiple of the step, B is added once below
            // using its original coordinates rather than a calculated approximation.
            if (distanceFromAMeters >= greatCircleDistanceMeters)
            {
                break;
            }

            var destination = geodesyService.CalculateDestinationPoint(
                request.LatA,
                request.LonA,
                initialBearingRadians,
                distanceFromAMeters);

            points.Add(new PathPoint(
                destination.Latitude,
                destination.Longitude,
                distanceFromAMeters));
        }

        points.Add(new PathPoint(
            request.LatB,
            request.LonB,
            greatCircleDistanceMeters));

        return points;
    }

    private static double CalculateEffectiveDistance(IReadOnlyList<ProfilePoint> points)
    {
        var totalEffectiveDistanceMeters = 0.0;

        for (var index = 1; index < points.Count; index++)
        {
            var previous = points[index - 1];
            var current = points[index];

            var horizontalDistanceMeters =
                current.DistanceFromAMeters - previous.DistanceFromAMeters;

            var elevationDifferenceMeters =
                current.ElevationMeters - previous.ElevationMeters;

            var segmentEffectiveDistanceMeters = Math.Sqrt(
                horizontalDistanceMeters * horizontalDistanceMeters +
                elevationDifferenceMeters * elevationDifferenceMeters);

            totalEffectiveDistanceMeters += segmentEffectiveDistanceMeters;
        }

        return totalEffectiveDistanceMeters;
    }

    private sealed record PathPoint(
        double Latitude,
        double Longitude,
        double DistanceFromAMeters);
}
