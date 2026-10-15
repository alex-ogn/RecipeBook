using RecipeBook.Models;

namespace RecipeBook.ViewModels.Recipes
{
    /// <summary>
    /// A user's profile page: their recipes and follow information.
    /// </summary>
    public class UserRecipesViewModel
    {
        public required ApplicationUser User { get; init; }
        public List<RecipeCardViewModel> Recipes { get; init; } = new();
        public bool IsOwnProfile { get; init; }
        public bool IsFollowing { get; init; }
        public int FollowersCount { get; init; }
        public int FollowingCount { get; init; }
    }
}
