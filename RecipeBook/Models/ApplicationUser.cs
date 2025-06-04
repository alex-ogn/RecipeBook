namespace RecipeBook.Models
{
    using Microsoft.AspNetCore.Identity;

    public class ApplicationUser : IdentityUser
    {
        public byte[]? ProfilePicture { get; set; }
        public int ProfilePictureVersion { get; set; } = 0;
        public ICollection<SavedRecipe> SavedRecipes { get; set; } = new List<SavedRecipe>();
        public ICollection<UserFollower> Followers { get; set; } = new List<UserFollower>();  // Тези, които ме следват
        public ICollection<UserFollower> Following { get; set; } = new List<UserFollower>();  // Тези, които аз следвам
        public ICollection<RecipeLike> LikedRecipes { get; set; } = new List<RecipeLike>();
        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    }
}
