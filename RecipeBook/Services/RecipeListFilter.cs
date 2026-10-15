using RecipeBook.ViewModels.Enums;

namespace RecipeBook.Services
{
    /// <summary>
    /// Criteria for recipe lists. Every criterion that is set must match.
    /// </summary>
    public class RecipeListFilter
    {
        public int? CategoryId { get; init; }
        public RecipeSortOption Sort { get; init; } = RecipeSortOption.Newest;

        /// <summary>Recipes written by this user.</summary>
        public string? AuthorId { get; init; }

        /// <summary>Recipes liked by this user.</summary>
        public string? LikedByUserId { get; init; }

        /// <summary>Recipes saved by this user.</summary>
        public string? SavedByUserId { get; init; }

        /// <summary>Recipes written by users this user follows.</summary>
        public string? FollowedByUserId { get; init; }

        /// <summary>Case-insensitive part of the title.</summary>
        public string? TitleContains { get; init; }
    }
}
