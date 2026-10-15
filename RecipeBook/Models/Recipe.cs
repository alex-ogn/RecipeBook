using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using RecipeBook.Resources;

namespace RecipeBook.Models
{
    public class Recipe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = nameof(DisplayTexts.Name), ResourceType = typeof(DisplayTexts))]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Instructions { get; set; }

        [Required]
        public int Servings { get; set; }

        [Required]
        [Display(Name = nameof(DisplayTexts.CookingTimeMinutes), ResourceType = typeof(DisplayTexts))]
        public int CookingTime { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Display(Name = nameof(DisplayTexts.Image), ResourceType = typeof(DisplayTexts))]
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        public int RecipeCategoryId { get; set; }

        [Display(Name = nameof(DisplayTexts.Category), ResourceType = typeof(DisplayTexts))]
        public RecipeCategory? RecipeCategory { get; set; }

        public int ViewCount { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = nameof(DisplayTexts.IsVegetarian), ResourceType = typeof(DisplayTexts))]
        public bool IsVegetarian { get; set; }

        [Display(Name = nameof(DisplayTexts.IsVegan), ResourceType = typeof(DisplayTexts))]
        public bool IsVegan { get; set; }

        [Display(Name = nameof(DisplayTexts.IsGlutenFree), ResourceType = typeof(DisplayTexts))]
        public bool IsGlutenFree { get; set; }

        [Display(Name = nameof(DisplayTexts.IsLactoseFree), ResourceType = typeof(DisplayTexts))]
        public bool IsLactoseFree { get; set; }

        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

        public ICollection<SavedRecipe> SavedByUsers { get; set; } = new List<SavedRecipe>();

        public ICollection<RecipeLike> Likes { get; set; } = new List<RecipeLike>();

        public ICollection<RecipeComment> Comments { get; set; } = new List<RecipeComment>();


    }
}
