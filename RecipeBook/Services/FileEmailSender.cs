namespace RecipeBook.Services
{
    /// <summary>
    /// Class for sending emails by writing them to files, useful for development and testing environments.
    /// </summary>
    public class FileEmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "SentEmails");
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, $"{Guid.NewGuid()}.html");

            var fileContent = $"<b>To:</b> {email}<br><b>Subject:</b> {subject}<hr>{htmlMessage}";
            await File.WriteAllTextAsync(filePath, fileContent);
        }
    }
}
