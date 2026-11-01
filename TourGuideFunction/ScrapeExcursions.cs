using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TourGuideFunction.Excursions;
using TourGuideFunction.Storage;

namespace TourGuideFunction
{
    public class ScrapeExcursions
    {
        private static readonly HttpClient Http = CreateHttpClient();

        private readonly ILogger _logger;
        private readonly EmailSender _emailSender;

        public ScrapeExcursions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ScrapeExcursions>();
            _emailSender = new EmailSender(loggerFactory.CreateLogger<EmailSender>());
        }

        [Function("ScrapeExcursions")]
        public async Task Run([TimerTrigger("0 0 12 * * *")] MyInfo myTimer)
        {
            // Config lives in tables, re-read every run, so edits apply without a restart.
            var store = new ExcursionStore(new TableServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")));
            await store.InitializeAsync();

            // Fail before scraping: results are only persisted once the report is sent.
            var settings = await store.GetEmailSettingsAsync();
            if (settings is null || string.IsNullOrWhiteSpace(settings.Sender) || string.IsNullOrWhiteSpace(settings.Recipients))
                throw new InvalidOperationException(
                    $"Email settings row ({ExcursionStore.SettingsTable} {EmailSettings.Partition}/{EmailSettings.Key}) is missing or has empty Sender/Recipients.");

            var monitor = new ExcursionMonitor(store, Http, _logger, delayBetweenRequests: TimeSpan.FromSeconds(5));
            await monitor.CheckAsync(DateTimeOffset.UtcNow, body => _emailSender.SendAsync(body, settings));
        }

        private static HttpClient CreateHttpClient()
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
            return http;
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus? ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
