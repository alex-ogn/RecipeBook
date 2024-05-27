using RecipeBook.Models;

namespace RecipeBook.Views.Recipes.ViewModels
{
    public class RecipeViewModel
    {
        public RecipeViewModel()
        {
            Recipe = new Recipe();
            Ingredients = new List<RecipeIngredientViewModel>();
        }

        public Recipe Recipe { get; set; }

        public List<RecipeIngredientViewModel> Ingredients { get; set; }

        public byte[] imageFile { get; set; }  // For holding the binary data of the image

    }
}
