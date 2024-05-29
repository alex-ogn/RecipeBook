using Microsoft.AspNetCore.Identity;
using RecipeBook.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace RecipeBook.Views.Recipes.ViewModels
{
    public class RecipeViewModel
    {
        public RecipeViewModel()
        {
            IngredientsByCategory = new Dictionary<string, RecipeIngredientViewModel[]>();
            SelectedIngredients = new List<RecipeIngredientViewModel>();
        }
        public RecipeViewModel(Recipe recipe)
        {
            this.Id= recipe.Id;
            this.Title= recipe.Title;
            this.Description= recipe.Description;
            this.Instructions= recipe.Instructions;
            this.Servings= recipe.Servings;
            this.CookingTime= recipe.CookingTime;
            this.UserId = recipe.UserId;
            this.imageFile = recipe.Image;

            IngredientsByCategory = new Dictionary<string, RecipeIngredientViewModel[]>();
            SelectedIngredients = new List<RecipeIngredientViewModel>();
        }

        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public string Instructions { get; set; }
        [Required]
        public int Servings { get; set; }
        [Required]
        [Display(Name = "Cooking Time ")]
        public TimeSpan CookingTime { get; set; }

        public string? UserId { get; set; }

        public byte[]? imageFile { get; set; }

        public byte[]? imageFileNew { get; set; }

        public List<RecipeIngredient>? RecipeIngredients { get; set; }

        public Dictionary<string, RecipeIngredientViewModel[]> IngredientsByCategory { get; set; }
        public List<RecipeIngredientViewModel> SelectedIngredients { get; set; } = new List<RecipeIngredientViewModel>();

        public void EditRecipe(Recipe recipe)
        {
            recipe.Id = this.Id;
            recipe.Title = this.Title;
            recipe.Description = this.Description;
            recipe.Instructions = this.Instructions;
            recipe.Servings = this.Servings;
            recipe.CookingTime = this.CookingTime;
            //recipe.UserId = this.UserId;
            //recipe.Image = this.Image;
        }

    }
}