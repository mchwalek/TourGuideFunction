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

        // The first stored price only sets the baseline. No threshold means any new low counts.
        public static bool ShouldNotify(int price, int? threshold, int? lowestSoFar) =>
            lowestSoFar is int lowest && price < lowest && (threshold is null || price < threshold);
    }
}
