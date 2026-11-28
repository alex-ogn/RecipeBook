namespace RecipeBook.Models
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Identity;
    using RecipeBook.Constants;

    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Public URL of the picture under <see cref="ImageConstants.ProfileImagesUrlPrefix"/>, or null for the
        /// default avatar (<see cref="ImageConstants.ProfilePictureOrDefault"/>).
        /// </summary>
        [StringLength(ImageConstants.MaxImageUrlLength)]
        public string? ProfilePictureUrl { get; set; }

        public ICollection<SavedRecipe> SavedRecipes { get; set; } = new List<SavedRecipe>();
        public ICollection<UserFollower> Followers { get; set; } = new List<UserFollower>();  // The users that follow this user
        public ICollection<UserFollower> Following { get; set; } = new List<UserFollower>();  // The users that this user follows
        public ICollection<RecipeLike> LikedRecipes { get; set; } = new List<RecipeLike>();
        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    }
}
