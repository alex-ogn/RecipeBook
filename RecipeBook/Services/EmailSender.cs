using Microsoft.Extensions.Options;
using RecipeBook.Options;

namespace RecipeBook.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public EmailSender(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using var client = new System.Net.Mail.SmtpClient(_settings.SmtpServer, _settings.Port)
            {
                Credentials = new System.Net.NetworkCredential(_settings.SenderEmail, _settings.Password),
                EnableSsl = true
            };

            var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(_settings.SenderEmail, "Recipe Book Support"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);
            await client.SendMailAsync(mailMessage);
        }
    }
}
