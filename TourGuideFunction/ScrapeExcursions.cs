using System;
using Azure.Communication.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using TourGuideFunction.Emails;
using TourGuideFunction.Excursions;

namespace TourGuideFunction
{
    public class ScrapeExcursions
    {
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

            try
            {
                var excursionUris = Environment.GetEnvironmentVariable("SCRAPED_EXCURSIONS")!.Split(',').Select(x => new Uri(x)).ToList();
                var excursions = new List<Excursion>(excursionUris.Count);

                using var browser = await Puppeteer.ConnectAsync(new ConnectOptions()
                {
                    DefaultViewport = new ViewPortOptions { Width = 1200 },
                    BrowserWSEndpoint = Environment.GetEnvironmentVariable("BROWSERLESS_CONNECTION_STRING"),
                });
                using var page = await browser.NewPageAsync();

                foreach (var uri in excursionUris)
                {
                    var excursion = await ExcursionScraper.GetExcursionAsync(page, uri);
                    excursions.Add(excursion);
                }

                await _emailSender.SendAsync(excursions);
            }
            catch(Exception ex)
            {
                _logger.LogError("exception: {Message}", ex.Message);
            }
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
