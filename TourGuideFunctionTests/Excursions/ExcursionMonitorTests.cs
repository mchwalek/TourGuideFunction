using System.Net;
using Azure.Data.Tables;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TourGuideFunction.Excursions;
using TourGuideFunction.Storage;
using TourGuideFunctionTests.Storage;

namespace TourGuideFunctionTests.Excursions
{
    // offer.html: "Grecja kontynentalna - dziedzictwo Hellady", 2027-06-23 - 2027-06-30, Katowice, 2597 zł.
    public class ExcursionMonitorTests : IAsyncLifetime
    {
        private readonly TableServiceClient _service = new("UseDevelopmentStorage=true");
        private readonly ExcursionStore _store;
        private readonly string _id = "test-" + Guid.NewGuid().ToString("N");
        private readonly string _url;
        private readonly ExcursionMonitor _monitor;
        private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

        public ExcursionMonitorTests()
        {
            _store = new ExcursionStore(_service);
            _url = $"https://r.pl/{_id}";
            var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Excursions", "Fixtures", "offer.html"));
            _monitor = new ExcursionMonitor(_store, new HttpClient(new StaticHtmlHandler(html)), NullLogger.Instance, delayBetweenRequests: TimeSpan.Zero);
        }

        public Task InitializeAsync() => _store.InitializeAsync();

        public async Task DisposeAsync()
        {
            await _service.GetTableClient(ExcursionStore.ExcursionsTable).DeleteEntityAsync(TrackedExcursion.Partition, _id);
            var points = _service.GetTableClient(ExcursionStore.PricePointsTable);
            await foreach (var p in points.QueryAsync<TableEntity>(e => e.PartitionKey == _id))
                await points.DeleteEntityAsync(p.PartitionKey, p.RowKey);
        }

        private Task TrackAsync(string expectedOrigin, int threshold) =>
            _service.GetTableClient(ExcursionStore.ExcursionsTable).AddEntityAsync(new TrackedExcursion
            {
                RowKey = _id,
                Url = _url,
                ExpectedDateFrom = "2027-06-23",
                ExpectedOrigin = expectedOrigin,
                Threshold = threshold,
                Enabled = true,
            });

        [AzuriteFact]
        public async Task CheckAsync_FirstPrice_StoresItWithoutReporting()
        {
            await TrackAsync("Katowice", threshold: 9999);

            var body = await _monitor.CheckAsync(Now);

            (body ?? "").Should().NotContain(_url);
            (await _store.GetLowestPriceAsync(_id)).Should().Be(2597);
        }

        [AzuriteFact]
        public async Task CheckAsync_NewLowBelowThreshold_ReportsDrop()
        {
            await TrackAsync("Katowice", threshold: 2600);
            await _store.AddPricePointAsync(PricePoint.Create(_id, Now.AddDays(-1),
                new Excursion("x", new DateOnly(2027, 6, 23), new DateOnly(2027, 6, 30), "Katowice", 3000)));

            var body = await _monitor.CheckAsync(Now);

            body.Should().Contain("2597 zł (poprzednio najniżej: 3000 zł)" + Environment.NewLine + _url);
            (await _store.GetLowestPriceAsync(_id)).Should().Be(2597);
        }

        [AzuriteFact]
        public async Task CheckAsync_OriginSwappedByRpl_ReportsUnavailableOnceAndStoresNoPrice()
        {
            await TrackAsync("Gdańsk", threshold: 9999);

            var first = await _monitor.CheckAsync(Now);
            var second = await _monitor.CheckAsync(Now.AddDays(1));

            first.Should().Contain("2027-06-23 | Wylot: Gdańsk" + Environment.NewLine + _url);
            (second ?? "").Should().NotContain(_url);
            (await _store.GetLowestPriceAsync(_id)).Should().BeNull();
        }

        private sealed class StaticHtmlHandler(string html) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html) });
        }
    }
}
