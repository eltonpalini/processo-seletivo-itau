namespace FraudMonitor.Domain.ValueObjects;

public record Location(
    double Latitude,
    double Longitude,
    string City = "",
    string State = "",
    string Country = "BR"
)
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Calculates the spherical distance in kilometers to another location using the Haversine formula.
    /// </summary>
    public double DistanceToInKm(Location other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        var lat1Rad = DegreesToRadians(Latitude);
        var lat2Rad = DegreesToRadians(other.Latitude);
        var deltaLat = DegreesToRadians(other.Latitude - Latitude);
        var deltaLon = DegreesToRadians(other.Longitude - Longitude);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }
}
