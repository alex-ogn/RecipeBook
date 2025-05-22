namespace RecipeBook.Models
{
    using Microsoft.AspNetCore.Identity;

    public class ApplicationUser : IdentityUser
    {
        public byte[]? ProfilePicture { get; set; }
        public int ProfilePictureVersion { get; set; } = 0;
        public ICollection<SavedRecipe> SavedRecipes { get; set; } = new List<SavedRecipe>();
    }
}
