using System;
using Azure.Communication.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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

                using var http = new HttpClient();

                foreach (var uri in excursionUris)
                {
                    var excursion = await ExcursionScraper.GetExcursionAsync(http, uri);
                    if (excursion is not null)
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
