using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using TourGuideFunction.Storage;

namespace TourGuideFunction
{
    public class EmailSender
    {
        private readonly ILogger _logger;
        public EmailSender(ILogger logger)
        {
            _logger = logger;
        }

        /// <param name="to">Comma-separated addresses.</param>
        public async Task SendAsync(string subject, string body, string from, string to)
        {
            var emailClient = new EmailClient(Environment.GetEnvironmentVariable("EMAIL_COMMUNICATION_CONNECTION_STRING"));

            var recipients = new EmailRecipients(
                to.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => new EmailAddress(x)));

            var message = new EmailMessage(
                from,
                recipients,
                new EmailContent(subject) { PlainText = body });
            var sendResult = await emailClient.SendAsync(Azure.WaitUntil.Completed, message);

            _logger.LogInformation("Email send status: {Status}", sendResult.Value.Status);
            if (sendResult.Value.Status != EmailSendStatus.Succeeded)
                throw new InvalidOperationException($"Email not sent: {sendResult.Value.Status}");
        }
    }
}
