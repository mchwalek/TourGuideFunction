using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TourGuideFunction
{
    public class EmailSender
    {
        private readonly ILogger _logger;
        public EmailSender(ILogger logger)
        {
            _logger = logger;
        }

        public  async Task SendAsync()
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
}
