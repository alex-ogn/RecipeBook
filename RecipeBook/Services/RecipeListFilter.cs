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

        /// <summary>When true, only recipes with an uploaded photo.</summary>
        public bool HasImage { get; init; }

        /// <summary>Recipes that take at most this many minutes.</summary>
        public int? MaxCookingTime { get; init; }

        // Diet flags: applied only when true (false means "any", not "must not be").
        public bool IsVegetarian { get; init; }
        public bool IsVegan { get; init; }
        public bool IsGlutenFree { get; init; }
        public bool IsLactoseFree { get; init; }
    }
}
