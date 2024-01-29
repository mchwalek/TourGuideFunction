using FluentAssertions;
using PuppeteerSharp;
using TourGuideFunction.Excursions;

namespace TourGuideFunctionTests.Excursions
{
    public class ExcursionScraperTests
    {
        [Theory]
        [InlineData("https://r.pl/w-blasku-mistycznego-ognia/zakwaterowanie-azb?czyCenaZaWszystkich=0&data=20240330&dlugoscPobytu=8&iataWyjazdu=WAW&liczbaPokoi=1&miastoWyjazdu=warszawa&wiek=1994-01-01&wiek=1994-01-01&wyzywienie=sniadania")]
        [InlineData("https://r.pl/arabia-saudyjska-niezwykle-krolestwo-saudow/zakwaterowanie-ara?czyCenaZaWszystkich=0&data=20240330&dlugoscPobytu=9&iataWyjazdu=WAW&liczbaPokoi=1&miastoWyjazdu=warszawa&wiek=1994-01-01&wiek=1994-01-01&wyzywienie=2-posilki")]
        public async Task GetExcurionAsync_ReturnsExcursion(string url)
        {
            await Puppeteer.CreateBrowserFetcher(new BrowserFetcherOptions()).DownloadAsync();
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions { DefaultViewport = new ViewPortOptions { Width = 1200 } });
            var page = await browser.NewPageAsync();

            var uri = new Uri(url);
            var excursion = await ExcursionScraper.GetExcursionAsync(page, uri);

            excursion.Should().NotBeNull();
            excursion.Name.Should().NotBeNullOrEmpty();
            excursion.DateFrom.Should().NotBe(default);
            excursion.DateTo.Should().NotBe(default);
            excursion.OriginCity.Should().NotBeNullOrEmpty();
            excursion.Price.Should().BePositive();
        }
    }
}