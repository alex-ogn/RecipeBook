using RecipeBook.Models;

namespace RecipeBook.Services
{
    public interface IRecipePdfService
    {
        byte[] GenerateRecipePdf(Recipe recipe);
    }
}
