using System.Text.RegularExpressions;

namespace TourGuideFunction.Excursions
{
    public static class PriceRules
    {
        // r.pl silently swaps unavailable offers for another date or airport.
        public static bool Matches(Excursion scraped, DateOnly expectedDateFrom, string expectedOrigin) =>
            scraped.DateFrom == expectedDateFrom
            && string.Equals(NormalizeSpaces(scraped.OriginCity), NormalizeSpaces(expectedOrigin), StringComparison.OrdinalIgnoreCase);

        // r.pl uses &nbsp; inside names like "Warszawa Chopin".
        private static string NormalizeSpaces(string value) => Regex.Replace(value, @"\s+", " ").Trim();

        // With a threshold, any new low below it counts, even the first price.
        // Without one, the first price only sets the baseline and any later new low counts.
        public static bool ShouldNotify(int price, int? threshold, int? lowestSoFar) =>
            threshold is int t
                ? price < t && (lowestSoFar is null || price < lowestSoFar)
                : lowestSoFar is int lowest && price < lowest;
    }
}
