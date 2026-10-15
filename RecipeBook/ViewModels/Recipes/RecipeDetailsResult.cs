using RecipeBook.Models;

namespace RecipeBook.ViewModels.Recipes
{
    public class RecipeDetailsResult
    {
        public required Recipe Recipe { get; init; }
        public List<RecipeCardViewModel> SimilarRecipes { get; init; } = new();
        public bool IsSaved { get; init; }
        public bool IsLiked { get; init; }
        public int LikeCount { get; init; }
    }
}
