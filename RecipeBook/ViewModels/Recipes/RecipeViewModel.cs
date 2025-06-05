using RecipeBook.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace RecipeBook.ViewModels.Recipes
{
    public class RecipeViewModel : Recipe
    {
        public RecipeViewModel(Recipe recipe, List<RecipeCardViewModel> SimilarRecipes)
        {
            Id = recipe.Id;
            Title = recipe.Title;
            Description = recipe.Description;
            Instructions = recipe.Instructions;
            Servings = recipe.Servings;
            CookingTime = recipe.CookingTime;
            UserId = recipe.UserId;
            User = recipe.User;
            Image = recipe.Image;
            RecipeIngredients = recipe.RecipeIngredients;
            RecipeCategory = recipe.RecipeCategory;
            ViewCount = recipe.ViewCount;
            Comments = recipe.Comments;
            IsVegetarian = recipe.IsVegetarian;
            IsVegan = recipe.IsVegan;
            IsGlutenFree = recipe.IsGlutenFree;
            IsLactoseFree = recipe.IsLactoseFree;
            this.SimilarRecipes = SimilarRecipes;
        }

        public List<RecipeCardViewModel> SimilarRecipes { get; set; } = new List<RecipeCardViewModel>();
    }
}
