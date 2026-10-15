namespace RecipeBook.Options
{
    /// <summary>
    /// SMTP settings bound from the "EmailSettings" section (user secrets / environment variables).
    /// </summary>
    public class EmailSettings
    {
        public const string SectionName = "EmailSettings";

        public string SmtpServer { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
