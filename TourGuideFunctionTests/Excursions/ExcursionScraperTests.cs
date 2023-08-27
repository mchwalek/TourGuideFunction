using FluentAssertions;
using TourGuideFunction.Excursions;

namespace TourGuideFunctionTests.Excursions
{
    public class ExcursionScraperTests
    {
        [Fact]
        public async Task GetExcurionAsync_ReturnsExcursion()
        {
            var uri = new Uri("https://r.pl/anglia-krolewska-samolotem/zakwaterowanie-ans?czyCenaZaWszystkich=0&data=20230903&dlugoscPobytu=6&iataWyjazdu=KTW&liczbaPokoi=1&miastoWyjazdu=katowice&wiek=1993-07-02&wiek=1993-07-02&wyzywienie=sniadania");
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