using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RecipeBook.Models
{
    public class Recipe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Име")]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Instructions { get; set; }

        [Required]
        public int Servings { get; set; }

        [Required]
        [Display(Name = "Време за готвене (в минути)")]
        public int CookingTime { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public byte[]? Image { get; set; }

        [Required]
        public int RecipeCategoryId { get; set; }

    //    [ForeignKey("RecipeCategoryId")]
        [Display(Name = "Категория")]
        public RecipeCategory? RecipeCategory { get; set; }

        public int ViewCount { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

        public ICollection<SavedRecipe> SavedByUsers { get; set; } = new List<SavedRecipe>();

        public ICollection<RecipeLike> Likes { get; set; } = new List<RecipeLike>();

        public ICollection<RecipeComment> Comments { get; set; } = new List<RecipeComment>();


    }
}
