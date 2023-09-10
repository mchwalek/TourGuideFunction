using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TourGuideFunction.Emails;
using TourGuideFunction.Excursions;

namespace TourGuideFunction
{
    public class EmailSender
    {
        private readonly ILogger _logger;
        public EmailSender(ILogger logger)
        {
            _logger = logger;
        }

        public  async Task SendAsync(IEnumerable<Excursion> excursions)
        {
            var emailClient = new EmailClient(Environment.GetEnvironmentVariable("EMAIL_COMMUNICATION_CONNECTION_STRING"));

            var sender = Environment.GetEnvironmentVariable("EMAIL_COMMUNICATION_SENDER");
            var recipients = new EmailRecipients(
                Environment.GetEnvironmentVariable("EMAIL_COMMUNICATION_RECIPIENTS")!.Split(',').Select(x => new EmailAddress(x)));
            
            var message = new EmailMessage(
                sender,
                recipients,
                new EmailContent("[TourGuide] ceny wycieczek") { PlainText = EmailFormatter.Format(excursions) });
            var sendResult = await emailClient.SendAsync(Azure.WaitUntil.Completed, message);

            _logger.LogInformation($"Email send status: {sendResult.Value.Status}");
        }
    }
}
