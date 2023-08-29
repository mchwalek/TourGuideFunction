using FluentAssertions;
using TourGuideFunction.Excursions;

namespace TourGuideFunctionTests.Excursions
{
    public class ExcursionScraperTests
    {
        [Theory]
        [InlineData("https://r.pl/anglia-krolewska-samolotem/zakwaterowanie-ans")]
        [InlineData("https://r.pl/mozaika-z-afrodyta/zakwaterowanie-cyb")]
        public async Task GetExcurionAsync_ReturnsExcursion(string url)
        {
            var uri = new Uri(url);
            var excursion = await ExcursionScraper.GetExcursionAsync(uri);

            excursion.Should().NotBeNull();
            excursion.Name.Should().NotBeNullOrEmpty();
            excursion.DateFrom.Should().NotBe(default);
            excursion.DateTo.Should().NotBe(default);
            excursion.OriginCity.Should().NotBeNullOrEmpty();
            excursion.Price.Should().BePositive();
        }
    }
}