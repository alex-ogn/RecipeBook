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
        /// Increments the view count of a tracked recipe, unless the viewer is its author.
        /// </summary>
        /// <param name="recipe">A recipe loaded (and tracked) by the current context.</param>
        /// <param name="viewerId">The current user's ID, or null for anonymous visitors.</param>
        /// <returns>True when the view was counted.</returns>
        Task<bool> RegisterViewAsync(Recipe recipe, string? viewerId);
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
