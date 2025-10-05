using RecipeBook.Models;

namespace RecipeBook.Services
{
    /// <summary>
    /// Interface for creating PDF files
    /// </summary>
    public interface IRecipePdfService
    {
        /// <summary>
        /// Generates pdf recipe
        /// </summary>
        /// <param name="recipe"></param>
        /// <returns></returns>
        byte[] GenerateRecipePdf(Recipe recipe);
    }
}
