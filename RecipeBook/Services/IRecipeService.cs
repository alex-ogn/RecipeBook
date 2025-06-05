using RecipeBook.Models;
using RecipeBook.ViewModels.Recipes;

namespace RecipeBook.Services
{
    public interface IRecipeService
    {
        Task<bool> DeleteRecipeAsync(int recipeId, string? currentUserId = null, bool forceDelete = false);

        Task<List<RecipeCardViewModel>> GetSimilarRecipesAsync(Recipe referenceRecipe, string currentUserId, bool allowEdit = false);
        Task<List<RecipeCardViewModel>> GetSuggestedRecipesForUserAsync(string userId, bool allowEdit = false);
    }

}
