using FluentAssertions;
using TourGuideFunction.Excursions;

namespace TourGuideFunctionTests.Excursions
{
    public class PriceRulesTests
    {
        private static readonly Excursion Scraped = new("Exc", new DateOnly(2027, 6, 23), new DateOnly(2027, 6, 30), "Katowice", 2597);

        [Fact]
        public void Matches_SameDateAndOrigin_ReturnsTrue() =>
            PriceRules.Matches(Scraped, new DateOnly(2027, 6, 23), "Katowice").Should().BeTrue();

        [Fact]
        public void Matches_OriginDiffersOnlyByCaseAndWhitespace_ReturnsTrue() =>
            PriceRules.Matches(Scraped, new DateOnly(2027, 6, 23), " katowice ").Should().BeTrue();

        [Fact]
        public void Matches_OriginWithNonBreakingSpace_ReturnsTrue() =>
            PriceRules.Matches(Scraped with { OriginCity = "Warszawa\u00A0Chopin" }, new DateOnly(2027, 6, 23), "Warszawa Chopin").Should().BeTrue();

        [Fact]
        public void Matches_DifferentDate_ReturnsFalse() =>
            PriceRules.Matches(Scraped, new DateOnly(2027, 6, 30), "Katowice").Should().BeFalse();

        [Fact]
        public void Matches_DifferentOrigin_ReturnsFalse() =>
            PriceRules.Matches(Scraped, new DateOnly(2027, 6, 23), "Warszawa Chopin").Should().BeFalse();

        [Fact]
        public void ShouldNotify_FirstPrice_ReturnsFalse() =>
            PriceRules.ShouldNotify(price: 1000, threshold: 5000, lowestSoFar: null).Should().BeFalse();

        [Fact]
        public void ShouldNotify_EqualToLowest_ReturnsFalse() =>
            PriceRules.ShouldNotify(price: 1000, threshold: 5000, lowestSoFar: 1000).Should().BeFalse();

        [Fact]
        public void ShouldNotify_NewLowButNotBelowThreshold_ReturnsFalse() =>
            PriceRules.ShouldNotify(price: 3000, threshold: 3000, lowestSoFar: 3500).Should().BeFalse();

        [Fact]
        public void ShouldNotify_NewLowBelowThreshold_ReturnsTrue() =>
            PriceRules.ShouldNotify(price: 2999, threshold: 3000, lowestSoFar: 3500).Should().BeTrue();

        [Fact]
        public void ShouldNotify_NoThresholdAndNewLow_ReturnsTrue() =>
            PriceRules.ShouldNotify(price: 9000, threshold: null, lowestSoFar: 9001).Should().BeTrue();

        [Fact]
        public void ShouldNotify_NoThresholdAndNotNewLow_ReturnsFalse() =>
            PriceRules.ShouldNotify(price: 9001, threshold: null, lowestSoFar: 9001).Should().BeFalse();

        [Fact]
        public void ShouldNotify_NoThresholdAndFirstPrice_ReturnsFalse() =>
            PriceRules.ShouldNotify(price: 9000, threshold: null, lowestSoFar: null).Should().BeFalse();
    }
}
