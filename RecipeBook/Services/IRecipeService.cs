using RecipeBook.Models;
using RecipeBook.ViewModels.Recipes;

namespace RecipeBook.Services
{
    /// <summary>
    /// Interface for recipe service
    /// </summary>
    public interface IRecipeService
    {
        /// <summary>
        /// Delete recipe
        /// </summary>
        /// <param name="recipeId"></param>
        /// <param name="currentUserId"></param>
        /// <param name="forceDelete"></param>
        /// <returns></returns>
        Task<bool> DeleteRecipeAsync(int recipeId, string? currentUserId = null, bool forceDelete = false);
        /// <summary>
        /// Selects similar recipes
        /// </summary>
        /// <param name="referenceRecipe"></param>
        /// <param name="currentUserId"></param>
        /// <param name="allowEdit"></param>
        /// <returns></returns>
        Task<List<RecipeCardViewModel>> GetSimilarRecipesAsync(Recipe referenceRecipe, string currentUserId, bool allowEdit = false);
        /// <summary>
        /// Selects suggested recipes for user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="allowEdit"></param>
        /// <returns></returns>
        Task<List<RecipeCardViewModel>> GetSuggestedRecipesForUserAsync(string userId, bool allowEdit = false);
    }
}
