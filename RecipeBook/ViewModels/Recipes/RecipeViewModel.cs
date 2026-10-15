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
            ImageUrl = recipe.ImageUrl;
            RecipeIngredients = recipe.RecipeIngredients;
            RecipeCategory = recipe.RecipeCategory;
            ViewCount = recipe.ViewCount;
            CreatedAt = recipe.CreatedAt;
            Comments = recipe.Comments;
            IsVegetarian = recipe.IsVegetarian;
            IsVegan = recipe.IsVegan;
            IsGlutenFree = recipe.IsGlutenFree;
            IsLactoseFree = recipe.IsLactoseFree;
            this.SimilarRecipes = SimilarRecipes;
        }

        public List<RecipeCardViewModel> SimilarRecipes { get; set; } = new List<RecipeCardViewModel>();

        public string? CurrentUserId { get; set; }
        public bool IsUserAdmin { get; set; }
        public bool IsLiked { get; set; }
        public bool IsSaved { get; set; }
        public int LikeCount { get; set; }

        /// <summary>
        /// The current user may edit or delete this recipe (author or Admin).
        /// </summary>
        public bool CanModify { get; set; }
    }
}
