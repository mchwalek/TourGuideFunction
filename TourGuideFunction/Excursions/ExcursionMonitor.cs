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

        /// <returns>Email body, or null when there is nothing to report.</returns>
        public async Task<string?> CheckAsync(DateTimeOffset now)
        {
            var drops = new List<PriceDrop>();
            var unavailable = new List<UnavailableExcursion>();
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
                            await _store.SetLastStatusAsync(tracked.RowKey, ExcursionStatus.Unavailable);
                        }
                        continue;
                    }

                    var lowest = await _store.GetLowestPriceAsync(tracked.RowKey);
                    await _store.AddPricePointAsync(PricePoint.Create(tracked.RowKey, now, scraped));
                    if (PriceRules.ShouldNotify(scraped.Price, tracked.Threshold, lowest))
                        drops.Add(new PriceDrop(scraped, lowest!.Value, tracked.Url));
                    if (tracked.LastStatus != ExcursionStatus.Available)
                        await _store.SetLastStatusAsync(tracked.RowKey, ExcursionStatus.Available);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Checking excursion {Id} failed", tracked.RowKey);
                }
            }

            return drops.Count == 0 && unavailable.Count == 0 ? null : EmailFormatter.Format(drops, unavailable);
        }
    }
}
