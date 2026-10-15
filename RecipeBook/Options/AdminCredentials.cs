namespace RecipeBook.Options
{
    /// <summary>
    /// Seeded administrator account bound from the "AdminCredentials" section (user secrets / environment variables).
    /// </summary>
    public class AdminCredentials
    {
        public const string SectionName = "AdminCredentials";

        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
