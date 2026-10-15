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
        /// Recipes (with author and likes) matching the filter, in the filter's sort order.
        /// </summary>
        Task<List<Recipe>> GetRecipesAsync(RecipeListFilter filter);

        /// <summary>
        /// The most liked recipes, then the most viewed.
        /// </summary>
        Task<List<Recipe>> GetMostPopularAsync(int count);

        Task<int> CountAsync();

        /// <summary>
        /// Everything the details page needs. The recipe is tracked so its view count can be updated.
        /// </summary>
        Task<RecipeDetailsResult?> GetDetailsAsync(int id, string? currentUserId);

        /// <summary>
        /// Recipe with category and ingredients, for the edit form.
        /// </summary>
        Task<Recipe?> GetForEditAsync(int id);

        /// <summary>
        /// Tracked recipe without related data, for updates.
        /// </summary>
        Task<Recipe?> GetByIdAsync(int id);

        /// <summary>
        /// Recipe with its author, for the delete confirmation page.
        /// </summary>
        Task<Recipe?> GetWithAuthorAsync(int id);

        /// <summary>
        /// Recipe with category and ingredients, for the PDF export.
        /// </summary>
        Task<Recipe?> GetForPdfAsync(int id);

        /// <summary>
        /// Sanitizes the rich text, resolves the ingredients (creating user-added ones) and saves the new recipe.
        /// </summary>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">An ingredient is invalid.</exception>
        Task CreateAsync(Recipe recipe, IReadOnlyCollection<RecipeIngredientViewModel> ingredients);

        /// <summary>
        /// Sanitizes the rich text, replaces the recipe's ingredients and saves the tracked recipe.
        /// </summary>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">An ingredient is invalid.</exception>
        Task UpdateAsync(Recipe recipe, IReadOnlyCollection<RecipeIngredientViewModel> ingredients);

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
        Task<List<RecipeCardViewModel>> GetSimilarRecipesAsync(Recipe referenceRecipe, string? currentUserId, bool allowEdit = false);

        /// <summary>
        /// Selects suggested recipes for user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="allowEdit"></param>
        /// <returns></returns>
        Task<List<RecipeCardViewModel>> GetSuggestedRecipesForUserAsync(string userId, bool allowEdit = false);
    }
}
