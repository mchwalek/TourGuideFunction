namespace TourGuideFunction.Excursions
{
    public record Excursion(string Name, DateOnly DateFrom, DateOnly DateTo, string OriginCity, int Price);
}
