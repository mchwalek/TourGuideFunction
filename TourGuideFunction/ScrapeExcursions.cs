using System;
using Azure.Communication.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TourGuideFunction
{
    public class ScrapeExcursions
    {
        private readonly ILogger _logger;

        public ScrapeExcursions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ScrapeExcursions>();
        }

        [Function("ScrapeExcursions")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] MyInfo myTimer)
        {
            var emailClient = new EmailClient(Environment.GetEnvironmentVariable("EMAIL_COMMUNICATION_CONNECTION_STRING"));

            var sender = Environment.GetEnvironmentVariable("EMAIL_COMMUNICATION_SENDER");
            var recipients = new EmailRecipients(
                Environment.GetEnvironmentVariable("EMAIL_COMMUNICATION_RECIPIENTS")!.Split(',').Select(x => new EmailAddress(x)));
            
            var message = new EmailMessage(
                sender,
                recipients,
                new EmailContent("[TourGuide] ceny wycieczek") { PlainText = "TODO" });
            var sendResult = await emailClient.SendAsync(Azure.WaitUntil.Completed, message);

            _logger.LogInformation($"Email send status: {sendResult.Value.Status}");
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
