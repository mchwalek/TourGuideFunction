using System.Text;
using TourGuideFunction.Excursions;

namespace TourGuideFunction.Emails
{
    public record PriceDrop(Excursion Excursion, int PreviousLow, string Url);

    public record UnavailableExcursion(string Url, string ExpectedDateFrom, string ExpectedOrigin);

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
                    stringBuilder.AppendLine($"{excursion.Name} | {excursion.DateFrom.ToString(dateFormat)} - {excursion.DateTo.ToString(dateFormat)} | Wylot: {excursion.OriginCity} | {excursion.Price} zł (poprzednio najniżej: {previousLow} zł)");
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
    }
}
