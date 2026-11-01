using FluentAssertions;
using TourGuideFunction.Excursions;

namespace TourGuideFunctionTests.Excursions
{
    public class ExcursionScraperTests
    {
        private static string Fixture(string name) =>
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Excursions", "Fixtures", name));

        [Fact]
        public void Parse_OfferPage_ReturnsExcursion()
        {
            var excursion = ExcursionScraper.Parse(Fixture("offer.html"));

            excursion.Should().Be(new Excursion(
                "Grecja kontynentalna - dziedzictwo Hellady",
                new DateOnly(2027, 6, 23),
                new DateOnly(2027, 6, 30),
                "Katowice",
                2597));
        }

        [Fact]
        public void Parse_PageWithoutOffer_ReturnsNull()
        {
            ExcursionScraper.Parse(Fixture("no-offer.html")).Should().BeNull();
        }

        [Fact]
        public void Parse_PageWithoutExcursionTitle_Throws()
        {
            var act = () => ExcursionScraper.Parse("<html><body><h1>Błąd</h1></body></html>");

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
