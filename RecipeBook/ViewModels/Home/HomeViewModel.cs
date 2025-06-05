using RecipeBook.ViewModels.Recipes;

namespace RecipeBook.ViewModels.Home
{
    public class HomeViewModel
    {
        public int CountUsers { get; set; }
        public int CountRecipes { get; set; }
        public List<RecipeCardViewModel> FeaturedRecipes { get; set; }
        public List<RecipeCardViewModel> Suggestions { get; set; } = new List<RecipeCardViewModel>();

    }
}
