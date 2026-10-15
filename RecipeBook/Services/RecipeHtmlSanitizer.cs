using Ganss.Xss;

namespace RecipeBook.Services
{
    /// <summary>
    /// Builds the sanitizer used for recipe descriptions and instructions (rich text from the editor).
    /// Registered once as a singleton; a configured HtmlSanitizer is safe to share between requests.
    /// </summary>
    public static class RecipeHtmlSanitizer
    {
        private static readonly string[] AllowedTags = { "b", "i", "ul", "ol", "li", "p", "span", "strong", "em", "br" };

        public static IHtmlSanitizer Create()
        {
            var sanitizer = new HtmlSanitizer();
            foreach (var tag in AllowedTags)
            {
                sanitizer.AllowedTags.Add(tag);
            }

            return sanitizer;
        }
    }
}
