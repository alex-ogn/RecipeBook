using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeBook.Models;
using RecipeBook.ViewModels.Enums;

namespace RecipeBook.ViewModels.Recipes
{
    public class RecipeFilterViewModel
    {
        public IEnumerable<RecipeCardViewModel> Recipes { get; set; }
        public SelectList Categories { get; set; }
        public int? SelectedCategoryId { get; set; }

        public RecipeSortOption SortOrder { get; set; } = RecipeSortOption.Newest;
    }
}
