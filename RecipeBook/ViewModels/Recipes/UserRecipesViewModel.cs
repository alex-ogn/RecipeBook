using RecipeBook.Models;
using RecipeBook.ViewModels.Shared;

namespace RecipeBook.ViewModels.Recipes
{
    /// <summary>
    /// A user's profile page: their recipes and follow information.
    /// </summary>
    public class UserRecipesViewModel
    {
        public required ApplicationUser User { get; init; }
        public required PagedResult<RecipeCardViewModel> Recipes { get; init; }
        public bool IsOwnProfile { get; init; }
        public bool IsFollowing { get; init; }
        public int FollowersCount { get; init; }
        public int FollowingCount { get; init; }
    }
}
