using RecipeBook.Models;

namespace RecipeBook.ViewModels.Users
{
    public class FollowViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public bool IsFollowing { get; set; }
    }
}
