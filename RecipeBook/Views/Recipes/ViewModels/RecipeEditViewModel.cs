using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeBook.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace RecipeBook.Views.Recipes.ViewModels
{
    public class RecipeEditViewModel
    {
        public RecipeEditViewModel()
        {
            IngredientsByCategory = new Dictionary<string, RecipeIngredientViewModel[]>();
            SelectedIngredients = new List<RecipeIngredientViewModel>();
        }
        public RecipeEditViewModel(Recipe recipe)
        {
            this.Id= recipe.Id;
            this.Title= recipe.Title;
            this.Description= recipe.Description;
            this.Instructions= recipe.Instructions;
            this.Servings= recipe.Servings;
            this.CookingTime= recipe.CookingTime;
            this.UserId = recipe.UserId;
            this.imageFile = recipe.Image;
            this.CategoryId = recipe.Category?.Id;

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
        [Range(1, 100, ErrorMessage = "Порциите трябва да са в диапазона между 1 и 100.")]
        [Display(Name = "Порции")]
        public int Servings { get; set; }
        //[Required]
        [Range(1, 300, ErrorMessage = "Времето за готвене трябва да е между 1 и 300 минути.")]
        [Display(Name = "Време за готвене (в минути)")]
        public int CookingTime { get; set; }

        public string? UserId { get; set; }

        public byte[]? imageFile { get; set; }

        public byte[]? imageFileNew { get; set; }

        public List<RecipeIngredient>? RecipeIngredients { get; set; }

        public Dictionary<string, RecipeIngredientViewModel[]> IngredientsByCategory { get; set; }
        public List<RecipeIngredientViewModel> SelectedIngredients { get; set; } = new List<RecipeIngredientViewModel>();

        [Display(Name = "Категория")]
        public int? CategoryId { get; set; }  // Holds the selected category ID

        [Display(Name = "Категория")]
        public SelectList Categories { get; set; }  // Used to populate the dropdown in the view

        public void EditRecipe(Recipe recipe)
        {
            recipe.Id = this.Id;
            recipe.Title = this.Title;
            recipe.Description = this.Description;
            recipe.Instructions = this.Instructions;
            recipe.Servings = this.Servings;
            recipe.CookingTime = this.CookingTime;


        }

    }
}