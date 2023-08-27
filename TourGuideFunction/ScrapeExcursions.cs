using System;
using Azure.Communication.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

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
        public async Task Run([TimerTrigger("0 */5 * * * *")] MyInfo myTimer)
        {
            await _emailSender.SendAsync();
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
