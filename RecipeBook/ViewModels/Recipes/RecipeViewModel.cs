using RecipeBook.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace RecipeBook.ViewModels.Recipes
{
    public class RecipeViewModel : Recipe
    {
        public RecipeViewModel(Recipe recipe)
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
            Category = recipe.Category;
            ViewCount = recipe.ViewCount;
            Comments = recipe.Comments;
        }
    }
}
