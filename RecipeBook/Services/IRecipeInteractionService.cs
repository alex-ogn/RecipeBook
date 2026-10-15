using RecipeBook.Models;

namespace RecipeBook.Services
{
    /// <summary>
    /// What users do with recipes: likes, saves and comments.
    /// </summary>
    public interface IRecipeInteractionService
    {
        /// <returns>False when the recipe does not exist.</returns>
        Task<bool> LikeAsync(int recipeId, string userId);
        Task UnlikeAsync(int recipeId, string userId);

        /// <returns>False when the recipe does not exist.</returns>
        Task<bool> SaveAsync(int recipeId, string userId);
        Task UnsaveAsync(int recipeId, string userId);

        /// <returns>False when the recipe does not exist.</returns>
        Task<bool> AddCommentAsync(int recipeId, string userId, string content);

        /// <summary>
        /// Tracked comment with its recipe and author, or null.
        /// </summary>
        Task<RecipeComment?> GetCommentAsync(int commentId);
        Task UpdateCommentAsync(RecipeComment comment, string content);
        Task DeleteCommentAsync(RecipeComment comment);
    }
}
