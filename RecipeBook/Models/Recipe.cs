﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RecipeBook.Models
{
    public class Recipe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public string Instructions { get; set; }
        [Required]
        public int Servings { get; set; }
        [Required]
        public TimeSpan CookingTime { get; set; }
        [Required]
        public string UserId { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public byte[]? Image { get; set; }

        public List<RecipeIngredient>? RecipeIngredients { get; set; }

        [Required]
        [ForeignKey("RecipeCategoryId")]
        [Display(Name = "Category")]
        public RecipeCategory Category { get; set; }

        public ICollection<SavedRecipe> SavedByUsers { get; set; } = new List<SavedRecipe>();

        public ICollection<RecipeLike> Likes { get; set; } = new List<RecipeLike>();

    }
}
