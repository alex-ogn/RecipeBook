using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeBook.Models;
using RecipeBook.Resources;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace RecipeBook.ViewModels.Recipes
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
            Id = recipe.Id;
            Title = recipe.Title;
            Description = recipe.Description;
            Instructions = recipe.Instructions;
            Servings = recipe.Servings;
            CookingTime = recipe.CookingTime;
            UserId = recipe.UserId;
            CategoryId = recipe.RecipeCategory?.Id;
            IsVegetarian = recipe.IsVegetarian;
            IsVegan = recipe.IsVegan;
            IsGlutenFree = recipe.IsGlutenFree;
            IsLactoseFree = recipe.IsLactoseFree;
            ImageUrl = recipe.ImageUrl;

            IngredientsByCategory = new Dictionary<string, RecipeIngredientViewModel[]>();
            SelectedIngredients = new List<RecipeIngredientViewModel>();
        }

        public int Id { get; set; }

        [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldCannotBeEmpty), ErrorMessageResourceType = typeof(ValidationTexts))]
        [Display(Name = nameof(DisplayTexts.Title), ResourceType = typeof(DisplayTexts))]
        [StringLength(100, MinimumLength = 1, ErrorMessageResourceName = nameof(ValidationTexts.TitleLength), ErrorMessageResourceType = typeof(ValidationTexts))]
        public string Title { get; set; }

        [Display(Name = nameof(DisplayTexts.Description), ResourceType = typeof(DisplayTexts))]
        public string? Description { get; set; }

        [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldCannotBeEmpty), ErrorMessageResourceType = typeof(ValidationTexts))]
        [Display(Name = nameof(DisplayTexts.Instructions), ResourceType = typeof(DisplayTexts))]
        public string Instructions { get; set; }
        [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldCannotBeEmpty), ErrorMessageResourceType = typeof(ValidationTexts))]
        [Range(1, 100, ErrorMessageResourceName = nameof(ValidationTexts.ServingsRange), ErrorMessageResourceType = typeof(ValidationTexts))]
        [Display(Name = nameof(DisplayTexts.Servings), ResourceType = typeof(DisplayTexts))]
        public int Servings { get; set; }

        [Range(1, 300, ErrorMessageResourceName = nameof(ValidationTexts.CookingTimeRange), ErrorMessageResourceType = typeof(ValidationTexts))]
        [Display(Name = nameof(DisplayTexts.CookingTimeMinutes), ResourceType = typeof(DisplayTexts))]
        public int CookingTime { get; set; }

        public string? UserId { get; set; }

        // Пътят към съществуващата снимка за показване във View-то
        public string? ImageUrl { get; set; }

        // Качването на новия файл от формата
        [Display(Name = nameof(DisplayTexts.NewImage), ResourceType = typeof(DisplayTexts))]
        public IFormFile? imageFileNew { get; set; }

        public List<RecipeIngredient>? RecipeIngredients { get; set; }

        public Dictionary<string, RecipeIngredientViewModel[]>? IngredientsByCategory { get; set; }
        public List<RecipeIngredientViewModel> SelectedIngredients { get; set; } = new List<RecipeIngredientViewModel>();

        [Display(Name = nameof(DisplayTexts.Category), ResourceType = typeof(DisplayTexts))]
        public int? CategoryId { get; set; }

        [Display(Name = nameof(DisplayTexts.Category), ResourceType = typeof(DisplayTexts))]
        public SelectList? Categories { get; set; }

        [Display(Name = nameof(DisplayTexts.IsVegetarian), ResourceType = typeof(DisplayTexts))]
        public bool IsVegetarian { get; set; }

        [Display(Name = nameof(DisplayTexts.IsVegan), ResourceType = typeof(DisplayTexts))]
        public bool IsVegan { get; set; }

        [Display(Name = nameof(DisplayTexts.IsGlutenFree), ResourceType = typeof(DisplayTexts))]
        public bool IsGlutenFree { get; set; }

        [Display(Name = nameof(DisplayTexts.IsLactoseFree), ResourceType = typeof(DisplayTexts))]
        public bool IsLactoseFree { get; set; }

        public void EditRecipe(Recipe recipe)
        {
            recipe.Id = Id;
            recipe.Title = Title;
            recipe.Description = Description;
            recipe.Instructions = Instructions;
            recipe.Servings = Servings;
            recipe.CookingTime = CookingTime;
            recipe.IsVegetarian = IsVegetarian;
            recipe.IsVegan = IsVegan;
            recipe.IsGlutenFree = IsGlutenFree;
            recipe.IsLactoseFree = IsLactoseFree;
        }

    }
}