using RecipeBook.Views.Recipes.ViewModels;

namespace RecipeBook.Views.Home.ViewModels
{
    public class HomeViewModel
    {
        public int CountUsers { get; set; }
        public int CountRecipes { get; set; }
        public List<RecipeCardViewModel> FeaturedRecipes { get; set; }
    }
}
