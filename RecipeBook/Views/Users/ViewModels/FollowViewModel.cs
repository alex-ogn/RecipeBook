using RecipeBook.Models;

namespace RecipeBook.Views.Users.ViewModels
{
    public class FollowViewModel
    {
        public ApplicationUser User { get; set; }
        public bool IsFollowing { get; set; }
    }
}
