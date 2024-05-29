using RecipeBook.Models;
using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Views.Recipes.ViewModels
{
    public class RecipeViewModel
    {
        public RecipeViewModel()
        {
            Recipe = new Recipe();
            IngredientsByCategory = new Dictionary<string, RecipeIngredientViewModel[]>();
            SelectedIngredients = new List<RecipeIngredientViewModel>();
        }

        public Recipe Recipe { get; set; }

        public Dictionary<string, RecipeIngredientViewModel[]> IngredientsByCategory { get; set; }
        public List<RecipeIngredientViewModel> SelectedIngredients { get; set; } = new List<RecipeIngredientViewModel>();

        public byte[] imageFile { get; set; }

    }
}