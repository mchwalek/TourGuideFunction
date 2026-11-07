using Azure.Data.Tables;
using FluentAssertions;
using TourGuideFunction.Excursions;
using TourGuideFunction.Storage;

namespace TourGuideFunctionTests.Storage
{
    // Runs against local Azurite; every test uses fresh ids and removes its rows.
    public class ExcursionStoreTests
    {
        private readonly TableServiceClient _service = new("UseDevelopmentStorage=true");
        private readonly ExcursionStore _store;

        public ExcursionStoreTests()
        {
            _store = new ExcursionStore(_service);
            _store.InitializeAsync().GetAwaiter().GetResult();
        }

        private static string NewId() => "test-" + Guid.NewGuid().ToString("N");

        private static TrackedExcursion Tracked(string id, bool enabled) => new()
        {
            RowKey = id,
            Url = "https://r.pl/x",
            ExpectedDateFrom = "2027-06-23",
            ExpectedOrigin = "Katowice",
            Threshold = 1234,
            Enabled = enabled,
        };

        private static readonly Excursion Scraped = new("Exc", new DateOnly(2027, 6, 23), new DateOnly(2027, 6, 30), "Katowice", 0);

        [AzuriteFact]
        public async Task GetEnabledExcursionsAsync_ReturnsOnlyEnabled()
        {
            var excursions = _service.GetTableClient(ExcursionStore.ExcursionsTable);
            string enabledId = NewId(), disabledId = NewId();
            await excursions.AddEntityAsync(Tracked(enabledId, enabled: true));
            await excursions.AddEntityAsync(Tracked(disabledId, enabled: false));
            try
            {
                var result = await _store.GetEnabledExcursionsAsync();

                result.Select(e => e.RowKey).Should().Contain(enabledId).And.NotContain(disabledId);
            }
            finally
            {
                await excursions.DeleteEntityAsync(TrackedExcursion.Partition, enabledId);
                await excursions.DeleteEntityAsync(TrackedExcursion.Partition, disabledId);
            }
        }

        [AzuriteFact]
        public async Task GetLowestPriceAsync_NoPricePoints_ReturnsNull()
        {
            var result = await _store.GetLowestPriceAsync(NewId());

            result.Should().BeNull();
        }

        [AzuriteFact]
        public async Task GetLowestPriceAsync_ReturnsMinimumOfStoredPrices()
        {
            var id = NewId();
            var start = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
            await _store.AddPricePointAsync(PricePoint.Create(id, start, Scraped with { Price = 3000 }));
            await _store.AddPricePointAsync(PricePoint.Create(id, start.AddDays(1), Scraped with { Price = 2500 }));
            await _store.AddPricePointAsync(PricePoint.Create(id, start.AddDays(2), Scraped with { Price = 2800 }));
            try
            {
                var result = await _store.GetLowestPriceAsync(id);

                result.Should().Be(2500);
            }
            finally
            {
                var points = _service.GetTableClient(ExcursionStore.PricePointsTable);
                await foreach (var p in points.QueryAsync<TableEntity>(e => e.PartitionKey == id))
                    await points.DeleteEntityAsync(p.PartitionKey, p.RowKey);
            }
        }

        [AzuriteFact]
        public async Task SetLastStatusAsync_ChangesOnlyLastStatus()
        {
            var excursions = _service.GetTableClient(ExcursionStore.ExcursionsTable);
            var id = NewId();
            await excursions.AddEntityAsync(Tracked(id, enabled: true));
            try
            {
                await _store.SetLastStatusAsync(id, ExcursionStatus.Unavailable);

                var stored = (await excursions.GetEntityAsync<TrackedExcursion>(TrackedExcursion.Partition, id)).Value;
                stored.LastStatus.Should().Be(ExcursionStatus.Unavailable);
                stored.Threshold.Should().Be(1234);
                stored.Url.Should().Be("https://r.pl/x");
            }
            finally
            {
                await excursions.DeleteEntityAsync(TrackedExcursion.Partition, id);
            }
        }
    }
}
