namespace TourGuideFunction.Excursions
{
    public static class PriceRules
    {
        // r.pl silently swaps unavailable offers for another date or airport.
        public static bool Matches(Excursion scraped, DateOnly expectedDateFrom, string expectedOrigin) =>
            scraped.DateFrom == expectedDateFrom
            && string.Equals(scraped.OriginCity.Trim(), expectedOrigin.Trim(), StringComparison.OrdinalIgnoreCase);

        // The first stored price only sets the baseline.
        public static bool ShouldNotify(int price, int threshold, int? lowestSoFar) =>
            lowestSoFar is int lowest && price < lowest && price < threshold;
    }
}
