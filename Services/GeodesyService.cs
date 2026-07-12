namespace RadioLinkSim.Services;

public sealed class GeodesyService
{
    public const double EarthRadiusMeters = 6_371_000.0;

    public double CalculateDistanceMeters(
        double latitudeA,
        double longitudeA,
        double latitudeB,
        double longitudeB)
    {
        var latitudeARadians = DegreesToRadians(latitudeA);
        var latitudeBRadians = DegreesToRadians(latitudeB);
        var latitudeDelta = DegreesToRadians(latitudeB - latitudeA);
        var longitudeDelta = DegreesToRadians(longitudeB - longitudeA);

        var haversine =
            Square(Math.Sin(latitudeDelta / 2.0)) +
            Math.Cos(latitudeARadians) *
            Math.Cos(latitudeBRadians) *
            Square(Math.Sin(longitudeDelta / 2.0));

        haversine = Math.Clamp(haversine, 0.0, 1.0);

        var angularDistance = 2.0 * Math.Atan2(
            Math.Sqrt(haversine),
            Math.Sqrt(1.0 - haversine));

        return EarthRadiusMeters * angularDistance;
    }

    public double CalculateInitialBearingRadians(
        double latitudeA,
        double longitudeA,
        double latitudeB,
        double longitudeB)
    {
        var latitudeARadians = DegreesToRadians(latitudeA);
        var latitudeBRadians = DegreesToRadians(latitudeB);
        var longitudeDelta = DegreesToRadians(longitudeB - longitudeA);

        var y = Math.Sin(longitudeDelta) * Math.Cos(latitudeBRadians);
        var x =
            Math.Cos(latitudeARadians) * Math.Sin(latitudeBRadians) -
            Math.Sin(latitudeARadians) * Math.Cos(latitudeBRadians) * Math.Cos(longitudeDelta);

        return Math.Atan2(y, x);
    }

    public (double Latitude, double Longitude) CalculateDestinationPoint(
        double startLatitude,
        double startLongitude,
        double initialBearingRadians,
        double distanceMeters)
    {
        var startLatitudeRadians = DegreesToRadians(startLatitude);
        var startLongitudeRadians = DegreesToRadians(startLongitude);
        var angularDistance = distanceMeters / EarthRadiusMeters;

        var destinationLatitudeRadians = Math.Asin(
            Math.Sin(startLatitudeRadians) * Math.Cos(angularDistance) +
            Math.Cos(startLatitudeRadians) * Math.Sin(angularDistance) * Math.Cos(initialBearingRadians));

        var destinationLongitudeRadians = startLongitudeRadians + Math.Atan2(
            Math.Sin(initialBearingRadians) * Math.Sin(angularDistance) * Math.Cos(startLatitudeRadians),
            Math.Cos(angularDistance) -
            Math.Sin(startLatitudeRadians) * Math.Sin(destinationLatitudeRadians));

        var destinationLatitude = RadiansToDegrees(destinationLatitudeRadians);
        var destinationLongitude = NormalizeLongitude(RadiansToDegrees(destinationLongitudeRadians));

        return (destinationLatitude, destinationLongitude);
    }

    private static double Square(double value) => value * value;

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;

    private static double NormalizeLongitude(double longitudeDegrees)
    {
        var normalized = (longitudeDegrees + 180.0) % 360.0;

        if (normalized < 0.0)
        {
            normalized += 360.0;
        }

        return normalized - 180.0;
    }
}
