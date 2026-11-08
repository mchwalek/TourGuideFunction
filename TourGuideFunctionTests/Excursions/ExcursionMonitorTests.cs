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
            _monitor = CreateMonitor(new StaticHtmlHandler(HttpStatusCode.OK, html));
        }

        private ExcursionMonitor CreateMonitor(HttpMessageHandler handler) =>
            new(_store, new HttpClient(handler), NullLogger.Instance, delayBetweenRequests: TimeSpan.Zero);

        private static readonly Func<string, Task> SendOk = _ => Task.CompletedTask;

        public Task InitializeAsync() => _store.InitializeAsync();

        public async Task DisposeAsync()
        {
            var points = _service.GetTableClient(ExcursionStore.PricePointsTable);
            foreach (var id in new[] { _id, SecondId })
            {
                await _service.GetTableClient(ExcursionStore.ExcursionsTable).DeleteEntityAsync(TrackedExcursion.Partition, id);
                await foreach (var p in points.QueryAsync<TableEntity>(e => e.PartitionKey == id))
                    await points.DeleteEntityAsync(p.PartitionKey, p.RowKey);
            }
        }

        // Sorts right after _id, so it is checked after it.
        private string SecondId => _id + "b";

        private Task TrackAsync(string expectedOrigin, int? threshold) => TrackAsync(_id, expectedOrigin, threshold);

        private Task TrackAsync(string id, string expectedOrigin, int? threshold) =>
            _service.GetTableClient(ExcursionStore.ExcursionsTable).AddEntityAsync(new TrackedExcursion
            {
                RowKey = id,
                Url = $"https://r.pl/{id}",
                ExpectedDateFrom = "2027-06-23",
                ExpectedOrigin = expectedOrigin,
                Threshold = threshold,
                Enabled = true,
            });

        [AzuriteFact]
        public async Task CheckAsync_FirstPriceWithoutThreshold_StoresItWithoutReporting()
        {
            await TrackAsync("Katowice", threshold: null);

            var (body, _) = await _monitor.CheckAsync(Now, SendOk, SendOk);

            (body ?? "").Should().NotContain(_url);
            (await _store.GetLowestPriceAsync(_id)).Should().Be(2597);
        }

        [AzuriteFact]
        public async Task CheckAsync_FirstPriceBelowThreshold_ReportsDrop()
        {
            await TrackAsync("Katowice", threshold: 9999);

            var (body, _) = await _monitor.CheckAsync(Now, SendOk, SendOk);

            body.Should().Contain("2597 zł (pierwsza cena)" + Environment.NewLine + _url);
            (await _store.GetLowestPriceAsync(_id)).Should().Be(2597);
        }

        [AzuriteFact]
        public async Task CheckAsync_NewLowBelowThreshold_ReportsDrop()
        {
            await TrackAsync("Katowice", threshold: 2600);
            await _store.AddPricePointAsync(PricePoint.Create(_id, Now.AddDays(-1),
                new Excursion("x", new DateOnly(2027, 6, 23), new DateOnly(2027, 6, 30), "Katowice", 3000)));

            var (body, _) = await _monitor.CheckAsync(Now, SendOk, SendOk);

            body.Should().Contain("2597 zł (poprzednio najniżej: 3000 zł)" + Environment.NewLine + _url);
            (await _store.GetLowestPriceAsync(_id)).Should().Be(2597);
        }

        [AzuriteFact]
        public async Task CheckAsync_NoThresholdConfigured_ReportsAnyNewLow()
        {
            await TrackAsync("Katowice", threshold: null);
            await _store.AddPricePointAsync(PricePoint.Create(_id, Now.AddDays(-1),
                new Excursion("x", new DateOnly(2027, 6, 23), new DateOnly(2027, 6, 30), "Katowice", 2598)));

            var (body, _) = await _monitor.CheckAsync(Now, SendOk, SendOk);

            body.Should().Contain("2597 zł (poprzednio najniżej: 2598 zł)" + Environment.NewLine + _url);
        }

        [AzuriteFact]
        public async Task CheckAsync_OriginSwappedByRpl_ReportsUnavailableOnceAndStoresNoPrice()
        {
            await TrackAsync("Gdańsk", threshold: 9999);

            var (first, _) = await _monitor.CheckAsync(Now, SendOk, SendOk);
            var (second, _) = await _monitor.CheckAsync(Now.AddDays(1), SendOk, SendOk);

            first.Should().Contain("2027-06-23 | Wylot: Gdańsk" + Environment.NewLine + _url);
            (second ?? "").Should().NotContain(_url);
            (await _store.GetLowestPriceAsync(_id)).Should().BeNull();
        }

        [AzuriteFact]
        public async Task CheckAsync_SendFails_PersistsNothingSoNextRunReportsAgain()
        {
            await TrackAsync("Katowice", threshold: 2600);
            await _store.AddPricePointAsync(PricePoint.Create(_id, Now.AddDays(-1),
                new Excursion("x", new DateOnly(2027, 6, 23), new DateOnly(2027, 6, 30), "Katowice", 3000)));

            var failedRun = () => _monitor.CheckAsync(Now, _ => throw new InvalidOperationException("email down"), SendOk);
            await failedRun.Should().ThrowAsync<InvalidOperationException>();
            (await _store.GetLowestPriceAsync(_id)).Should().Be(3000);

            var (body, _) = await _monitor.CheckAsync(Now.AddDays(1), SendOk, SendOk);
            body.Should().Contain("2597 zł (poprzednio najniżej: 3000 zł)");
        }

        [AzuriteFact]
        public async Task CheckAsync_HttpError_SendsFailureOnceToSupportOnly()
        {
            await TrackAsync("Katowice", threshold: 9999);
            var monitor = CreateMonitor(new StaticHtmlHandler(HttpStatusCode.Forbidden, ""));

            var first = await monitor.CheckAsync(Now, SendOk, SendOk);
            var second = await monitor.CheckAsync(Now.AddDays(1), SendOk, SendOk);

            first.Errors.Should().Contain("Błąd sprawdzania:").And.Contain("403").And.Contain(_url);
            first.Report.Should().BeNull();
            (second.Errors ?? "").Should().NotContain(_url);
        }

        [AzuriteFact]
        public async Task CheckAsync_SupportSendFails_SendsNoReportAndPersistsNothing()
        {
            await TrackAsync("Katowice", threshold: 9999);
            await TrackAsync(SecondId, "Katowice", threshold: 9999);
            var reportSent = false;
            var monitor = CreateMonitor(new FailingSecondRequestHandler(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Excursions", "Fixtures", "offer.html"))));

            var failedRun = () => monitor.CheckAsync(Now, _ => { reportSent = true; return Task.CompletedTask; },
                _ => throw new InvalidOperationException("email down"));

            await failedRun.Should().ThrowAsync<InvalidOperationException>();
            reportSent.Should().BeFalse();
            (await _store.GetLowestPriceAsync(_id)).Should().BeNull();
        }

        [AzuriteFact]
        public async Task CheckAsync_RowDeletedDuringRun_StillSavesOtherExcursions()
        {
            await TrackAsync("Gdańsk", threshold: 9999);
            await TrackAsync(SecondId, "Katowice", threshold: 9999);
            await _store.AddPricePointAsync(PricePoint.Create(SecondId, Now.AddDays(-1),
                new Excursion("x", new DateOnly(2027, 6, 23), new DateOnly(2027, 6, 30), "Katowice", 3000)));

            await _monitor.CheckAsync(Now, _ =>
                _service.GetTableClient(ExcursionStore.ExcursionsTable).DeleteEntityAsync(TrackedExcursion.Partition, _id), SendOk);

            (await _store.GetLowestPriceAsync(SecondId)).Should().Be(2597);
        }

        private sealed class StaticHtmlHandler(HttpStatusCode status, string html) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(html) });
        }

        private sealed class FailingSecondRequestHandler(string html) : HttpMessageHandler
        {
            private int _requests;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(++_requests == 1
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html) }
                    : new HttpResponseMessage(HttpStatusCode.Forbidden));
        }
    }
}
