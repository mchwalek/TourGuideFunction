using FluentAssertions;
using TourGuideFunction.Emails;
using TourGuideFunction.Excursions;

namespace TourGuideFunctionTests.Emails
{
    public class EmailFormatterTests
    {
        private static readonly PriceDrop Drop = new(
            new Excursion("Exc1", new DateOnly(2023, 9, 1), new DateOnly(2023, 9, 2), "Katowice", 123),
            PreviousLow: 150,
            Url: "https://r.pl/exc1");

        private static readonly UnavailableExcursion Unavailable = new("https://r.pl/exc2", "2023-10-01", "Warszawa Chopin");

        private static string Lines(params string[] lines) => string.Concat(lines.Select(l => l + Environment.NewLine));

        private static readonly FailedExcursion Failed = new("https://r.pl/exc3", "Response status code does not indicate success: 403 (Forbidden).");

        [Fact]
        public void Format_PriceDrops_ListsNewPriceAndPreviousLow()
        {
            var result = EmailFormatter.Format(new[] { Drop }, Array.Empty<UnavailableExcursion>());

            result.Should().Be(Lines(
                "Spadek ceny:",
                "Exc1 | 2023-09-01 - 2023-09-02 | Wylot: Katowice | 123 zł (poprzednio najniżej: 150 zł)",
                "https://r.pl/exc1"));
        }

        [Fact]
        public void Format_FirstPriceDrop_SaysItIsTheFirstPrice()
        {
            var result = EmailFormatter.Format(new[] { Drop with { PreviousLow = null } }, Array.Empty<UnavailableExcursion>());

            result.Should().Contain("123 zł (pierwsza cena)");
        }

        [Fact]
        public void Format_UnavailableExcursions_ListsConfiguredDateAndOrigin()
        {
            var result = EmailFormatter.Format(Array.Empty<PriceDrop>(), new[] { Unavailable });

            result.Should().Be(Lines(
                "Niedostępne w skonfigurowanym terminie/wylocie:",
                "2023-10-01 | Wylot: Warszawa Chopin",
                "https://r.pl/exc2"));
        }

        [Fact]
        public void Format_DropsAndUnavailable_SeparatesSectionsWithBlankLine()
        {
            var result = EmailFormatter.Format(new[] { Drop }, new[] { Unavailable });

            result.Should().Be(Lines(
                "Spadek ceny:",
                "Exc1 | 2023-09-01 - 2023-09-02 | Wylot: Katowice | 123 zł (poprzednio najniżej: 150 zł)",
                "https://r.pl/exc1",
                "",
                "Niedostępne w skonfigurowanym terminie/wylocie:",
                "2023-10-01 | Wylot: Warszawa Chopin",
                "https://r.pl/exc2"));
        }

        [Fact]
        public void FormatFailures_ListsErrorAndUrl()
        {
            var result = EmailFormatter.FormatFailures(new[] { Failed });

            result.Should().Be(Lines(
                "Błąd sprawdzania:",
                "Response status code does not indicate success: 403 (Forbidden).",
                "https://r.pl/exc3"));
        }
    }
}
