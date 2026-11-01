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

        [Fact]
        public void Parse_ConfiguratorMissingPrice_Throws()
        {
            var html = Fixture("offer.html").Replace("r-typography:kartaHotelu-konfigurator-cenaPodsumowanie:cenaZaOsobe\"", "renamed\"");

            var act = () => ExcursionScraper.Parse(html);

            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData("https://example.com/grecja/hotel")]
        [InlineData("https://r.pl/api/oferty")]
        [InlineData("https://r.pl/szukaj?kraj=grecja")]
        [InlineData("https://r.pl/rezerwacja/123")]
        public async Task GetExcursionAsync_UrlNotAllowedByRobots_ThrowsWithoutRequest(string url)
        {
            var handler = new CountingHandler();

            var act = () => ExcursionScraper.GetExcursionAsync(new HttpClient(handler), new Uri(url));

            await act.Should().ThrowAsync<InvalidOperationException>();
            handler.Requests.Should().Be(0);
        }

        private sealed class CountingHandler : HttpMessageHandler
        {
            public int Requests { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests++;
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("") });
            }
        }
    }
}
