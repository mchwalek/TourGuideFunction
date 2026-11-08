using System.Globalization;
using Microsoft.Extensions.Logging;
using TourGuideFunction.Emails;
using TourGuideFunction.Storage;

namespace TourGuideFunction.Excursions
{
    public class ExcursionMonitor
    {
        private readonly ExcursionStore _store;
        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly TimeSpan _delayBetweenRequests;

        public ExcursionMonitor(ExcursionStore store, HttpClient http, ILogger logger, TimeSpan delayBetweenRequests)
        {
            _store = store;
            _http = http;
            _logger = logger;
            _delayBetweenRequests = delayBetweenRequests;
        }

        /// <summary>Nothing is persisted until both emails succeed, so a failed email is retried next run.</summary>
        /// <returns>The bodies sent; null where there was nothing to send.</returns>
        public async Task<(string? Report, string? Errors)> CheckAsync(DateTimeOffset now, Func<string, Task> sendReport, Func<string, Task> sendErrors)
        {
            var drops = new List<PriceDrop>();
            var unavailable = new List<UnavailableExcursion>();
            var failed = new List<FailedExcursion>();
            var writes = new List<Func<Task>>();
            var excursions = await _store.GetEnabledExcursionsAsync();

            for (var i = 0; i < excursions.Count; i++)
            {
                if (i > 0)
                    await Task.Delay(_delayBetweenRequests);

                var tracked = excursions[i];
                try
                {
                    var expectedDateFrom = DateOnly.ParseExact(tracked.ExpectedDateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var scraped = await ExcursionScraper.GetExcursionAsync(_http, new Uri(tracked.Url));

                    if (scraped is null || !PriceRules.Matches(scraped, expectedDateFrom, tracked.ExpectedOrigin))
                    {
                        _logger.LogInformation("Excursion {Id} unavailable, r.pl offered {Scraped}", tracked.RowKey, scraped);
                        if (tracked.LastStatus != ExcursionStatus.Unavailable)
                        {
                            unavailable.Add(new UnavailableExcursion(tracked.Url, tracked.ExpectedDateFrom, tracked.ExpectedOrigin));
                            writes.Add(() => _store.SetLastStatusAsync(tracked.RowKey, ExcursionStatus.Unavailable));
                        }
                        continue;
                    }

                    var lowest = await _store.GetLowestPriceAsync(tracked.RowKey);
                    writes.Add(() => _store.AddPricePointAsync(PricePoint.Create(tracked.RowKey, now, scraped)));
                    if (PriceRules.ShouldNotify(scraped.Price, tracked.Threshold, lowest))
                        drops.Add(new PriceDrop(scraped, lowest, tracked.Url));
                    if (tracked.LastStatus != ExcursionStatus.Available)
                        writes.Add(() => _store.SetLastStatusAsync(tracked.RowKey, ExcursionStatus.Available));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Checking excursion {Id} failed", tracked.RowKey);
                    if (tracked.LastStatus != ExcursionStatus.Error)
                    {
                        failed.Add(new FailedExcursion(tracked.Url, ex.Message));
                        writes.Add(() => _store.SetLastStatusAsync(tracked.RowKey, ExcursionStatus.Error));
                    }
                }
            }

            // Errors go first: if the support email fails, users are not sent a report that would repeat next run.
            string? errors = null;
            if (failed.Count > 0)
            {
                errors = EmailFormatter.FormatFailures(failed);
                await sendErrors(errors);
            }

            string? report = null;
            if (drops.Count > 0 || unavailable.Count > 0)
            {
                report = EmailFormatter.Format(drops, unavailable);
                await sendReport(report);
            }

            // The report is already sent: one failed write (e.g. row deleted mid-run) must not skip the others.
            foreach (var write in writes)
            {
                try
                {
                    await write();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Saving excursion state failed");
                }
            }

            return (report, errors);
        }
    }
}
