using System.Text;
using TourGuideFunction.Excursions;

namespace TourGuideFunction.Emails
{
    /// <param name="PreviousLow">Null when this is the first price seen.</param>
    public record PriceDrop(Excursion Excursion, int? PreviousLow, string Url);

    public record UnavailableExcursion(string Url, string ExpectedDateFrom, string ExpectedOrigin);

    public record FailedExcursion(string Url, string Error);

    public static class EmailFormatter
    {
        public static string Format(IReadOnlyCollection<PriceDrop> drops, IReadOnlyCollection<UnavailableExcursion> unavailable)
        {
            const string dateFormat = "yyyy-MM-dd";

            var stringBuilder = new StringBuilder();

            if (drops.Count > 0)
            {
                stringBuilder.AppendLine("Spadek ceny:");
                foreach (var (excursion, previousLow, url) in drops)
                {
                    var previous = previousLow is int low ? $"poprzednio najniżej: {low} zł" : "pierwsza cena";
                    stringBuilder.AppendLine($"{excursion.Name} | {excursion.DateFrom.ToString(dateFormat)} - {excursion.DateTo.ToString(dateFormat)} | Wylot: {excursion.OriginCity} | {excursion.Price} zł ({previous})");
                    stringBuilder.AppendLine(url);
                }
            }

            if (unavailable.Count > 0)
            {
                if (drops.Count > 0)
                    stringBuilder.AppendLine();
                stringBuilder.AppendLine("Niedostępne w skonfigurowanym terminie/wylocie:");
                foreach (var (url, expectedDateFrom, expectedOrigin) in unavailable)
                {
                    stringBuilder.AppendLine($"{expectedDateFrom} | Wylot: {expectedOrigin}");
                    stringBuilder.AppendLine(url);
                }
            }

            return stringBuilder.ToString();
        }

        public static string FormatFailures(IReadOnlyCollection<FailedExcursion> failed)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Błąd sprawdzania:");
            foreach (var (url, error) in failed)
            {
                stringBuilder.AppendLine(error);
                stringBuilder.AppendLine(url);
            }

            return stringBuilder.ToString();
        }
    }
}
