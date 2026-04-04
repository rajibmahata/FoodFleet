namespace FoodFleet.Domain.Interfaces.Services;

public interface IGeoService
{
    double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2);
    bool IsWithinRadius(double branchLat, double branchLng, double deliveryLat, double deliveryLng, double radiusKm);
}
