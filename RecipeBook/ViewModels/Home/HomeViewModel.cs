using RecipeBook.ViewModels.Recipes;

namespace RecipeBook.ViewModels.Home
{
    public class HomeViewModel
    {
        public int CountUsers { get; set; }
        public int CountRecipes { get; set; }

        /// <summary>
        /// The banner's background slideshow: recipes featured by an admin, or by default the most liked ones with a photo.
        /// </summary>
        public List<RecipeCardViewModel> HeroSlides { get; set; } = new();

        public List<RecipeCardViewModel> PopularRecipes { get; set; } = new();
        public List<RecipeCardViewModel> NewestRecipes { get; set; } = new();
        public List<RecipeCardViewModel> Suggestions { get; set; } = new();
        public List<CategoryTileViewModel> Categories { get; set; } = new();

        /// <summary>Signed-out visitors also see the "how it works" steps and the join panel.</summary>
        public bool IsSignedIn { get; set; }
    }
}
